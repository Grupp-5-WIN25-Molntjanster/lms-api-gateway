namespace Lms.ApiGateway.Configuration;

/// <summary>
/// Strongly-typed JWT configuration loaded from appsettings.json section "Jwt".
/// Uses the IOptions pattern for validation and binding.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The symmetric secret key used to sign and validate JWT tokens.
    /// Must be at least 32 characters for HMAC-SHA256.
    /// In production, this comes from Azure Key Vault or environment variables.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// The expected issuer claim in the JWT.
    /// Tokens from other issuers will be rejected.
    /// </summary>
    public string Issuer { get; set; } = "lms-auth-service";

    /// <summary>
    /// The expected audience claim in the JWT.
    /// Tokens intended for other audiences will be rejected.
    /// </summary>
    public string Audience { get; set; } = "lms-api";

    /// <summary>
    /// Access token lifetime in minutes.
    /// Kept short for security; refresh tokens handle long-lived sessions.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;
}