using System.Diagnostics;

namespace Lms.ApiGateway.Middleware;

/// <summary>
/// Middleware that ensures every request has a unique Correlation ID.
/// This ID is forwarded to downstream services and included in logs,
/// enabling distributed tracing across all microservices.
/// 
/// If the client sends an X-Correlation-Id header, it is reused.
/// Otherwise, a new GUID is generated.
/// The ID is added to the response so clients can reference it.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if client already sent a correlation ID
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingId))
        {
            context.TraceIdentifier = existingId!;
        }
        else
        {
            // Generate new correlation ID
            var correlationId = Guid.NewGuid().ToString("N");
            context.TraceIdentifier = correlationId;
            context.Request.Headers[CorrelationIdHeader] = correlationId;
        }

        // Add to response headers so client can reference it
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        // Add to logging scope
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register the CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}