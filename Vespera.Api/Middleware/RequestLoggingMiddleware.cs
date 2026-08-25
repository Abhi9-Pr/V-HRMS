using System.Diagnostics;
using Vespera.Application.Abstractions.Identity;

namespace Vespera.Api.Middleware;

/// <summary>
/// Logs method/path/status/duration/correlation id/tenant id/user id only — request and response
/// bodies are never logged. That's the PII-scrubbing rule for this middleware: not selective
/// field redaction (which silently misses whatever field you didn't think to redact), but simply
/// never capturing body content in the first place.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private static readonly Action<ILogger, string, string, int, long, string?, Exception?> LogRequest = LoggerMessage.Define<string, string, int, long, string?>(
        LogLevel.Information, new EventId(1, nameof(LogRequest)), "{Method} {Path} responded {StatusCode} in {ElapsedMs}ms (tenant={TenantId})");

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Deliberately NOT an ITenantContext parameter here: this middleware runs before
    // UseAuthentication(), and ASP.NET Core resolves every InvokeAsync parameter before the
    // method body runs (i.e. before next() is even called) — that would permanently freeze the
    // scoped ITenantContext as unauthenticated for the rest of the request. Resolving it manually
    // after next() has completed is what makes it see the real, authenticated tenant — see the
    // CAUTION note on HttpTenantContext.
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        var tenantId = tenantContext.HasTenant ? tenantContext.TenantId.Value.ToString() : null;

        LogRequest(
            _logger, context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, tenantId, null);
    }
}
