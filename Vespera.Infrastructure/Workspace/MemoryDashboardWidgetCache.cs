using Microsoft.Extensions.Caching.Memory;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.Workspace;

/// <summary>In-process cache backing <see cref="IDashboardWidgetCache"/>. Fine for a single-node
/// deployment; a multi-node deployment would swap this for an <c>IDistributedCache</c>-backed
/// implementation behind the same port, per AGENTS.md's OCP rule — nothing above this class would
/// need to change.</summary>
public sealed class MemoryDashboardWidgetCache : IDashboardWidgetCache
{
    private readonly IMemoryCache _cache;

    public MemoryDashboardWidgetCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryGet(string key, out object? value) => _cache.TryGetValue(key, out value);

    public void Set(string key, object? value, TimeSpan ttl) => _cache.Set(key, value, ttl);
}
