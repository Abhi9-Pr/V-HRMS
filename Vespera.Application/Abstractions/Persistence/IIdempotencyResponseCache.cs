namespace Vespera.Application.Abstractions.Persistence;

public sealed record CachedHttpResponse(int StatusCode, string ContentType, string Body);

/// <summary>
/// Backs the HTTP-level idempotency-replay middleware: caches a full response so a repeated
/// request carrying the same <c>Idempotency-Key</c> gets back the exact original response instead
/// of re-executing the handler. Distinct from <see cref="IIdempotencyStore"/> (Phase 3's
/// MediatR-pipeline "reject a duplicate command" store, which has no response body to replay) —
/// see the Phase 4 plan for why these are two independent mechanisms, not one collapsed into the
/// other. Implementations persist immediately (there is no surrounding
/// <c>TransactionBehavior</c> at the HTTP-middleware layer to defer to).
/// </summary>
public interface IIdempotencyResponseCache
{
    public Task<CachedHttpResponse?> GetAsync(string idempotencyKey, CancellationToken cancellationToken);

    public Task StoreAsync(string idempotencyKey, CachedHttpResponse response, CancellationToken cancellationToken);
}
