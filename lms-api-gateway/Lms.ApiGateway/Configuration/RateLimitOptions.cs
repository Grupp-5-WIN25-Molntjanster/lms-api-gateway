namespace Lms.ApiGateway.Configuration;

/// <summary>
/// Rate limiting configuration loaded from appsettings.json section "RateLimiting".
/// Uses fixed window algorithm for simplicity and predictability.
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// 100 requests per 10 seconds is generous for a web app but prevents abuse.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// The time window in seconds for the rate limit.
    /// After this window, the counter resets.
    /// </summary>
    public int WindowInSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests that can be queued when the limit is exceeded.
    /// 0 means no queueing – excess requests get 503 immediately.
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}