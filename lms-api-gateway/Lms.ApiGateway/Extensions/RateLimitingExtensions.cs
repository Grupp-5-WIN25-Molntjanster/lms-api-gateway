using Lms.ApiGateway.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace Lms.ApiGateway.Extensions;

public static class RateLimitingExtensions
{
    public static void AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitSection = configuration.GetSection(RateLimitOptions.SectionName);
        var rateLimitOptions = rateLimitSection.Get<RateLimitOptions>()!;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(policyName: "Fixed", config =>
            {
                config.PermitLimit = rateLimitOptions.PermitLimit;
                config.Window = TimeSpan.FromSeconds(rateLimitOptions.WindowInSeconds);
                config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                config.QueueLimit = rateLimitOptions.QueueLimit;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var message = new
                {
                    error = "rate_limit_exceeded",
                    message = $"Too many requests. Limit: {rateLimitOptions.PermitLimit} per {rateLimitOptions.WindowInSeconds} seconds.",
                    retryAfterSeconds = rateLimitOptions.WindowInSeconds
                };

                await context.HttpContext.Response.WriteAsJsonAsync(message, cancellationToken);
            };
        });
    }
}