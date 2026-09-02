using Vespera.Application.Abstractions.Services;

namespace Vespera.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[ICorrelationIdProvider.HttpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (context.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("CorrelationId").BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}
