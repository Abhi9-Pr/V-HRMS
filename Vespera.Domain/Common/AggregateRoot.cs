namespace Vespera.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Optimistic-concurrency token. App-managed (not a provider-native rowversion/xmin) so it
    /// behaves identically across every supported database engine; the persistence layer
    /// regenerates it on every insert/update and a mismatch on save surfaces as a typed
    /// concurrency <see cref="Result"/> error, never a raw provider exception.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
