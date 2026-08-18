namespace Vespera.Domain.Common;

public interface IHasDomainEvents
{
    public IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    public void ClearDomainEvents();
}
