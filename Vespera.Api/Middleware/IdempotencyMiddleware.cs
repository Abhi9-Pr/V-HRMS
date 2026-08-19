using Vespera.Application.Abstractions.Persistence;

namespace Vespera.Api.Middleware;

/// <summary>
/// Mobile affordance #1 (see docs/api-mobile-contract.md): opt-in on the <c>Idempotency-Key</c>
/// header for mutating requests. A repeated request with the same key gets back the exact
/// original response — status, content type, and body — without the handler running again.
/// Backed by <see cref="IIdempotencyResponseCache"/>, which is distinct from the MediatR-pipeline
/// <see cref="IIdempotencyStore"/> (Phase 3's "reject a duplicate command" behavior).
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";

    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyResponseCache cache)
    {
        if (!MutatingMethods.Contains(context.Request.Method) ||
            !context.Request.Headers.TryGetValue(HeaderName, out var keyValues) ||
            string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            await _next(context);
            return;
        }

        var key = keyValues.First()!;
        var cached = await cache.GetAsync(key, context.RequestAborted);

        if (cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(buffer, leaveOpen: true);
            var body = await reader.ReadToEndAsync(context.RequestAborted);

            if (context.Response.StatusCode is >= 200 and < 300)
            {
                await cache.StoreAsync(
                    key,
                    new CachedHttpResponse(context.Response.StatusCode, context.Response.ContentType ?? "application/json", body),
                    context.RequestAborted);
            }

            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }
}
