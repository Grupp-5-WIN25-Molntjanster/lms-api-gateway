using Microsoft.AspNetCore.Diagnostics;

namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for mapping minimal API endpoints.
/// These are the only direct endpoints the gateway exposes:
/// - /health : Health check for Azure App Service / load balancers
/// - /error  : Global exception handler target
/// 
/// All other routes are handled by YARP reverse proxy.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps the /health endpoint.
    /// Used by Azure App Service for health probes and load balancer checks.
    /// Returns 200 OK with service metadata.
    /// 
    /// This endpoint is intentionally anonymous – health checks should
    /// not require authentication.
    /// </summary>
    public static void MapHealthCheck(this WebApplication app)
    {
        app.MapGet("/health", () =>
        {
            return Results.Ok(new
            {
                Status = "Healthy",
                Service = "API Gateway",
                Version = "1.0.0",
                Timestamp = DateTimeOffset.UtcNow.ToString("o"),
                Environment = app.Environment.EnvironmentName
            });
        })
        .AllowAnonymous()
        .WithTags("Health")
        .WithDisplayName("Health Check")
        .WithDescription("Returns the health status of the API Gateway. Used by Azure for health probes.");
    }

    /// <summary>
    /// Maps the /error endpoint for global exception handling.
    /// This is the target of app.UseExceptionHandler("/error").
    /// 
    /// When an unhandled exception occurs anywhere in the pipeline,
    /// ASP.NET Core re-executes the request on this endpoint.
    /// We log the error and return a generic 500 response without
    /// exposing internal details to the client (security best practice).
    /// </summary>
    public static void MapErrorEndpoint(this WebApplication app)
    {
        app.Map("/error", (HttpContext context) =>
        {
            // Get the exception from the IExceptionHandlerFeature
            var exceptionFeature = context.Features
                .Get<IExceptionHandlerFeature>();

            // Get logger from DI
            var logger = context.RequestServices
                .GetRequiredService<ILogger<Program>>();

            // Log the full exception for internal diagnostics
            logger.LogError(
                exceptionFeature?.Error,
                "Unhandled exception in API Gateway. Path: {Path}, Method: {Method}",
                exceptionFeature?.Path ?? "unknown",
                context.Request.Method);

            // Return generic error to client (no internal details exposed)
            return Results.Problem(
                detail: "An unexpected error occurred. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
        })
        .AllowAnonymous()
        .WithTags("Error")
        .WithDisplayName("Error Handler")
        .WithDescription("Handles unhandled exceptions. Returns a safe, generic error response.");
    }
}