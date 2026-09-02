namespace Vespera.Application.Abstractions.Services;

/// <summary>The ambient correlation id, if one exists. Null outside an HTTP request with no
/// caller-supplied id yet resolved — a hosted service's own DI scope has no ambient request, so
/// code reachable from both a request and a background job (e.g. <c>EfOutboxWriter</c>) must treat
/// this as optional rather than assume it's always set.</summary>
public interface ICorrelationIdProvider
{
    /// <summary>Key <c>CorrelationIdMiddleware</c> stores the id under in <c>HttpContext.Items</c>
    /// — shared here (rather than duplicated as a magic string in Infrastructure) so the
    /// Infrastructure-layer implementation of this port can read the same key without Infrastructure
    /// referencing Api, which the dependency rule forbids.</summary>
    public const string HttpContextItemKey = "CorrelationId";

    public string? Current { get; }
}
