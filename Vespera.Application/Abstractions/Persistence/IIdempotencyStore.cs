namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// Backing store for <c>IdempotencyBehavior</c>. Not part of the originally enumerated port
/// list, but the behavior has nothing to check against without it.
/// </summary>
public interface IIdempotencyStore
{
    public Task<bool> HasBeenProcessedAsync(string idempotencyKey, CancellationToken cancellationToken);

    public Task MarkAsProcessedAsync(string idempotencyKey, CancellationToken cancellationToken);
}
