using FluentAssertions;
using Vespera.Domain.Assets;
using Vespera.Domain.Assets.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Assets;

public class AssetAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Assign_Should_Raise_AssetAssigned()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);

        assignment.DomainEvents.Should().ContainSingle(e => e is AssetAssigned);
    }

    [Fact]
    public void Return_Should_Set_ReturnedAt_And_Condition()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);

        var result = assignment.Return("Good condition", Now.AddDays(30));

        result.IsSuccess.Should().BeTrue();
        assignment.ReturnedAt.Should().Be(Now.AddDays(30));
    }

    [Fact]
    public void Return_Should_Fail_When_Already_Returned()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), Now);
        assignment.Return("Good", Now.AddDays(30));

        var result = assignment.Return("Good again", Now.AddDays(31));

        result.IsFailure.Should().BeTrue();
    }
}
