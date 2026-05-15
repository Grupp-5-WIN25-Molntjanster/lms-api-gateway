using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Lms.ApiGateway.Configuration;

namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring JWT Bearer authentication.
/// This is the primary security mechanism for the entire LMS system.
/// 
/// Validation rules applied:
/// 1. Issuer must match (which service created the token)
/// 2. Audience must match (which service the token is for)
/// 3. Lifetime must be valid (token not expired)
/// 4. Signing key must be valid (token not tampered with)
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication to the service collection.
    /// The JWT secret is read from configuration (environment variable in production).
    /// </summary>
    public static void AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>()!;

        // Validate that secret is configured
        if (string.IsNullOrEmpty(jwtOptions.Secret))
        {
            throw new InvalidOperationException(
                "JWT Secret is not configured. " +
                "Set it via environment variable 'Jwt__Secret' or in User Secrets. " +
                "Never hardcode secrets in appsettings.json!");
        }

        services.Configure<JwtOptions>(jwtSection);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // === Issuer Validation ===
                // Ensures the token was issued by our Auth Service
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                // === Audience Validation ===
                // Ensures the token is intended for our API
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                // === Lifetime Validation ===
                // Ensures the token has not expired
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30), // 30 second tolerance for clock drift

                // === Signature Validation ===
                // Ensures the token was signed with our secret key
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.Secret)),

                // === Claim Mapping ===
                // Map standard JWT claims to .NET claim types
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            // === Event Hooks for Logging ===
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(context.Exception,
                        "JWT authentication failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("JWT token validated successfully for user");
                    return Task.CompletedTask;
                }
            };
        });
    }
}