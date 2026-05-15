using Lms.ApiGateway.Extensions;
using Lms.ApiGateway.Middleware;
using Serilog;

// ============================================================
// 1. Bootstrap Serilog (must be first)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // 2. Configure Serilog from appsettings + extensions
    // ============================================================
    builder.AddLogging();

    // ============================================================
    // 3. Add services to the DI container
    // ============================================================
    builder.Services.AddGatewayCors(builder.Configuration);
    builder.Services.AddGatewayRateLimiting(builder.Configuration);
    builder.Services.AddGatewayAuthentication(builder.Configuration);
    builder.Services.AddGatewayAuthorization();
    builder.Services.AddGatewayReverseProxy(builder.Configuration);

    var app = builder.Build();

    // ============================================================
    // 4. Configure middleware pipeline (ORDER MATTERS!)
    // ============================================================

    // 4a. Serilog request logging (capture all HTTP traffic)
    app.UseSerilogRequestLogging();

    // 4b. Correlation ID (adds X-Correlation-Id for distributed tracing)
    app.UseCorrelationId();

    // 4c. Exception handler (catches unhandled exceptions, returns safe error)
    app.UseExceptionHandler("/error");

    // 4d. CORS (must come before Authentication)
    app.UseCors();

    // 4e. Rate limiting (protect backend from abuse)
    app.UseRateLimiter();

    // 4f. Authentication (validates JWT)
    app.UseAuthentication();

    // 4g. Authorization (checks roles/policies)
    app.UseAuthorization();

    // 4h. Claims forwarding (extracts JWT claims → HTTP headers for downstream)
    app.UseClaimsForwarding();

    // 4i. Custom request logging (logs method, path, status, duration)
    app.UseRequestLogging();

    // 4j. Health check endpoint
    app.MapHealthCheck();

    // 4k. Error endpoint
    app.MapErrorEndpoint();

    // 4l. YARP Reverse Proxy 
    app.MapReverseProxy();

    // ============================================================
    // 5. Start the gateway
    // ============================================================
    Log.Information("API Gateway starting on {Urls}...", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}