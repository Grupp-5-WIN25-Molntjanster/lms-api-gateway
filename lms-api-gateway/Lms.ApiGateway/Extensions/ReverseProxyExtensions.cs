namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring YARP Reverse Proxy.
/// 
/// YARP loads its configuration from the "ReverseProxy" section of appsettings.json.
/// This includes:
/// - Routes: Which URL patterns to match
/// - Clusters: Where to forward matched requests
/// - Transforms: How to modify the request before forwarding
/// 
/// The PathRemovePrefix transform strips the route prefix so that:
///   Client request:  /auth/login
///   After transform: /api/auth/login
///   Forwarded to:    https://auth-service.azurewebsites.net/api/auth/login
/// </summary>
public static class ReverseProxyExtensions
{
    /// <summary>
    /// Adds YARP reverse proxy to the service collection.
    /// Configuration is loaded from the "ReverseProxy" section.
    /// </summary>
    public static void AddGatewayReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));
    }
}