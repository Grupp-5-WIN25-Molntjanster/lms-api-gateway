using Lms.ApiGateway.Authorization;

namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring role-based authorization policies.
/// Policies are reusable rules that can be applied to routes via [Authorize] attribute
/// or YARP route configuration.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Adds authorization policies to the service collection.
    /// Each policy corresponds to a specific access level.
    /// </summary>
    public static void AddGatewayAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            // Any authenticated user (all roles)
            .AddPolicy(Policies.Authenticated, policy =>
                policy.RequireAuthenticatedUser())

            // Instructor or Admin roles
            .AddPolicy(Policies.InstructorOrAdmin, policy =>
                policy.RequireRole(Policies.Roles.Instructor, Policies.Roles.Admin))

            // Admin role only
            .AddPolicy(Policies.AdminOnly, policy =>
                policy.RequireRole(Policies.Roles.Admin));
    }
}