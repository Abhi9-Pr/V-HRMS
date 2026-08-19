namespace Vespera.Application.Abstractions.Messaging;

/// <summary>
/// Opt-in marker for mutating commands that should be safe to retry. IdempotencyBehavior only
/// acts on requests that implement this and supply a non-empty key.
/// </summary>
public interface IIdempotentRequest
{
    public string? IdempotencyKey { get; }
}
