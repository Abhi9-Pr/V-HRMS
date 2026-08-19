using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class AggregateRootTests
{
    [Fact]
    public void Raise_Should_Add_The_Event_To_DomainEvents()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());

        aggregate.DoSomething();

        aggregate.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<TestDomainEvent>();
    }

    [Fact]
    public void ClearDomainEvents_Should_Empty_The_Collection()
    {
        var aggregate = new TestAggregateRoot(Guid.NewGuid());
        aggregate.DoSomething();

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    private sealed record TestDomainEvent(DateTimeOffset OccurredOn) : DomainEvent(OccurredOn);

    private sealed class TestAggregateRoot(Guid id) : AggregateRoot<Guid>(id)
    {
        public void DoSomething() => Raise(new TestDomainEvent(DateTimeOffset.UtcNow));
    }
}
