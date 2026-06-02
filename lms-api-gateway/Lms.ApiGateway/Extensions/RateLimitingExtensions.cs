using Lms.ApiGateway.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Lms.ApiGateway.Extensions;

public static class RateLimitingExtensions
{
    public static void AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        // Get rate limit configuration from appsettings.json
        var rateLimitSection = configuration.GetSection(RateLimitOptions.SectionName);
        var rateLimitOptions = rateLimitSection.Get<RateLimitOptions>();

        // Use default values if section doesn't exist
        if (rateLimitOptions == null)
        {
            rateLimitOptions = new RateLimitOptions();
        }

        services.AddRateLimiter(options =>
        {
            // Global rate limiting policy
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitOptions.WindowInSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = rateLimitOptions.QueueLimit
                    }));

            // Custom rejection response
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Handle rate limit exceeded
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = rateLimitOptions.WindowInSeconds;

                var message = new
                {
                    error = "rate_limit_exceeded",
                    message = $"Too many requests. Limit: {rateLimitOptions.PermitLimit} requests per {rateLimitOptions.WindowInSeconds} seconds.",
                    retryAfterSeconds = retryAfter,
                    timestamp = DateTime.UtcNow
                };

                await context.HttpContext.Response.WriteAsJsonAsync(message, cancellationToken);
            };
        });
    }
}