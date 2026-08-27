namespace Vespera.Application.Abstractions.Services;

/// <summary>Short-TTL cache for one widget's rendered payload, keyed by whatever the caller
/// composes (typically tenant+user+widget key) — deliberately dumb (get/set, no invalidation
/// API) because dashboard widgets tolerate a few seconds of staleness and SignalR pushes the
/// truly time-sensitive bits (punch state, approvals count, announcements) independently of
/// this cache. See <c>GetDashboardQueryHandler</c>.</summary>
public interface IDashboardWidgetCache
{
    public bool TryGet(string key, out object? value);

    public void Set(string key, object? value, TimeSpan ttl);
}
