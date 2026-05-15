using Serilog;

namespace Lms.ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring Serilog structured logging.
/// Serilog is the de facto standard for structured logging in .NET.
/// It outputs JSON-compatible log entries that can be ingested by
/// Azure Monitor, Elasticsearch, Datadog, etc.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Serilog to the WebApplicationBuilder.
    /// Reads configuration from the "Serilog" section of appsettings.json.
    /// </summary>
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName();
        });
    }
}