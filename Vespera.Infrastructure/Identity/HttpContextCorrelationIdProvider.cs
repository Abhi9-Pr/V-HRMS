using Microsoft.AspNetCore.Http;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Identity;

/// <summary>Reads the id <c>CorrelationIdMiddleware</c> stashed in <c>HttpContext.Items</c> —
/// outermost in the pipeline, so by the time anything else in the request resolves this, it's
/// already set. Same capture-once-per-scope shape as <see cref="HttpTenantContext"/>: safe here
/// because nothing runs before <c>CorrelationIdMiddleware</c> that could resolve this first.
/// Outside an HTTP request (a hosted service's own DI scope), <c>HttpContext</c> is null and
/// <see cref="Current"/> is simply null — callers already have to handle that per the interface's
/// own contract.</summary>
public sealed class HttpContextCorrelationIdProvider : ICorrelationIdProvider
{
    public HttpContextCorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        Current = httpContextAccessor.HttpContext?.Items.TryGetValue(ICorrelationIdProvider.HttpContextItemKey, out var value) == true
            ? value as string
            : null;
    }

    public string? Current { get; }
}
