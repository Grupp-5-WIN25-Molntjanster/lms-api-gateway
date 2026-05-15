using System.Security.Claims;

namespace Lms.ApiGateway.Middleware;

/// <summary>
/// Middleware that extracts JWT claims and forwards them as HTTP headers
/// to downstream microservices.
/// 
/// This follows the "claims propagation" pattern commonly used in API gateways.
/// Downstream services can use these headers without re-parsing the JWT,
/// BUT they should still validate the JWT for defense in depth.
/// 
/// Forwarded headers:
/// - X-User-Id: The authenticated user's ID (from "sub" claim)
/// - X-User-Email: The authenticated user's email
/// - X-User-Roles: The authenticated user's roles (comma-separated)
/// - X-User-Name: The authenticated user's full name
/// </summary>
public class ClaimsForwardingMiddleware
{
    private readonly RequestDelegate _next;

    public ClaimsForwardingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only forward claims if the user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claims = context.User.Claims;

            // User ID (sub claim = unique identifier)
            var userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                      ?? claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (userId != null)
                context.Request.Headers["X-User-Id"] = userId;

            // Email
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                     ?? claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (email != null)
                context.Request.Headers["X-User-Email"] = email;

            // Roles (comma-separated for easy parsing)
            var roles = claims.Where(c => c.Type == ClaimTypes.Role
                                       || c.Type == "role"
                                       || c.Type == "roles")
                              .Select(c => c.Value);
            if (roles.Any())
                context.Request.Headers["X-User-Roles"] = string.Join(",", roles);

            // Full name
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "name")?.Value;
            if (name != null)
                context.Request.Headers["X-User-Name"] = name;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method to register the ClaimsForwardingMiddleware.
/// </summary>
public static class ClaimsForwardingMiddlewareExtensions
{
    public static IApplicationBuilder UseClaimsForwarding(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ClaimsForwardingMiddleware>();
    }
}