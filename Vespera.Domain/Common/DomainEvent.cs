namespace Vespera.Domain.Common;

public abstract record DomainEvent(DateTimeOffset OccurredOn)
{
    public Guid EventId { get; } = Guid.NewGuid();
}
