using Lms.ApiGateway.Configuration;

namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring CORS (Cross-Origin Resource Sharing).
/// 
/// Security principle: Only allow requests from known, trusted origins.
/// Never use AllowAnyOrigin() in production – it defeats the purpose of CORS.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds CORS policy that only allows configured origins.
    /// Supports credentials (cookies, Authorization header) for JWT tokens.
    /// </summary>
    public static void AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection(CorsOptions.SectionName);
        var corsOptions = corsSection.Get<CorsOptions>()!;

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials() // Required for Authorization header with JWT
                    .WithExposedHeaders("X-Correlation-Id"); // Expose for debugging
            });
        });
    }
}