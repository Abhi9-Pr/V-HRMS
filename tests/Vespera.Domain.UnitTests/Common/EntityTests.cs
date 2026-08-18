using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class EntityTests
{
    [Fact]
    public void Entities_With_The_Same_Id_And_Type_Should_Be_Equal()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Entities_With_Different_Ids_Should_Not_Be_Equal()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Should().NotBe(second);
    }

    [Fact]
    public void Entities_Of_Different_Types_With_The_Same_Id_Should_Not_Be_Equal()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new OtherTestEntity(id);

        first.Equals(second).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Should_Be_Consistent_With_Equals()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherTestEntity(Guid id) : Entity<Guid>(id);
}
