namespace Lms.ApiGateway.Configuration;

/// <summary>
/// Combined gateway configuration for strongly-typed access.
/// Not directly bound from appsettings – used to aggregate sub-options.
/// </summary>
public class GatewayOptions
{
    public JwtOptions Jwt { get; set; } = new();
    public CorsOptions Cors { get; set; } = new();
    public RateLimitOptions RateLimiting { get; set; } = new();
}