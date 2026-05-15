namespace Lms.ApiGateway.Configuration;

/// <summary>
/// CORS configuration loaded from appsettings.json section "Cors".
/// </summary>
public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// List of allowed origins that can make cross-origin requests.
    /// In production, this is the Vercel deployment URL.
    /// In development, it includes localhost.
    /// NEVER use wildcard "*" in production.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}