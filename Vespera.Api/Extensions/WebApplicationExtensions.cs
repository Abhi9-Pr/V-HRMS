using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vespera.Api.Hubs;
using Vespera.Api.Middleware;

namespace Vespera.Api.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>Order: correlation id (outermost, so every later stage can log against it) →
    /// unhandled-exception ProblemDetails → security headers → CORS → rate limiting →
    /// compression → request logging → authentication → authorization → idempotency replay
    /// (innermost — it needs to know who's calling and whether the request is even allowed
    /// through before it's worth caching a response for).</summary>
    public static WebApplication UseVesperaMiddlewarePipeline(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseCors(ObservabilityServiceCollectionExtensions.CorsPolicyName);
        app.UseRateLimiter();
        app.UseResponseCompression();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<IdempotencyMiddleware>();

        return app;
    }

    public static WebApplication MapVesperaEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHub<NotificationHub>("/hubs/notifications");

        // Excluded from the OpenAPI doc (and so from the generated TypeScript client) — these are
        // operational endpoints, not part of the versioned API surface a feature client calls.
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync,
        }).ExcludeFromDescription();

        app.MapGet("/version", () => Results.Ok(new { version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown" }))
            .ExcludeFromDescription();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    private static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString(), description = e.Value.Description }),
        });
    }
}
