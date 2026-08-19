namespace Vespera.Application.Abstractions.Persistence;

/// <summary>
/// Thrown by an <see cref="IUnitOfWork"/> implementation when a save fails because a row's
/// concurrency token no longer matches (someone else updated or deleted it first) — Infrastructure's
/// translation of its own provider-specific concurrency exception (e.g. EF Core's
/// <c>DbUpdateConcurrencyException</c>) into something Application is allowed to reference.
/// <see cref="Vespera.Application.Behaviors.TransactionBehavior{TRequest,TResponse}"/> catches this and turns it into
/// a typed <c>Result</c> Conflict error, per AGENTS.md — never a raw provider exception at the
/// handler/API boundary.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
