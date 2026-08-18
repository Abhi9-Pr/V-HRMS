using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class ReportingRelationshipTests
{
    [Fact]
    public void Create_Should_Fail_When_Employee_Reports_To_Themselves()
    {
        var employeeId = EmployeeId.New();

        var result = ReportingRelationship.Create(
            TenantId.New(), employeeId, employeeId, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_Second_Overlapping_Reporting_Line_For_The_Same_Employee_Should_Be_Rejected()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();

        var original = ReportingRelationship.Create(
            tenantId, employeeId, EmployeeId.New(), new DateOnly(2026, 1, 1), null).Value;

        var replacement = ReportingRelationship.Create(
            tenantId, employeeId, EmployeeId.New(), new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<ReportingRelationshipId, ReportingRelationship>(
            [original], replacement);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Closing_The_Original_Line_Allows_A_New_NonOverlapping_Line_To_Be_Added()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();

        var original = ReportingRelationship.Create(
            tenantId, employeeId, EmployeeId.New(), new DateOnly(2026, 1, 1), null).Value;
        original.EndOn(new DateOnly(2026, 5, 31));

        var replacement = ReportingRelationship.Create(
            tenantId, employeeId, EmployeeId.New(), new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<ReportingRelationshipId, ReportingRelationship>(
            [original], replacement);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AsOf_Should_Resolve_The_Manager_Active_On_A_Given_Date()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();
        var firstManager = EmployeeId.New();
        var secondManager = EmployeeId.New();

        var first = ReportingRelationship.Create(tenantId, employeeId, firstManager, new DateOnly(2026, 1, 1), new DateOnly(2026, 5, 31)).Value;
        var second = ReportingRelationship.Create(tenantId, employeeId, secondManager, new DateOnly(2026, 6, 1), null).Value;

        var activeManager = EffectiveDatedTimeline.AsOf<ReportingRelationshipId, ReportingRelationship>(
            [first, second], new DateOnly(2026, 7, 1));

        activeManager.Should().NotBeNull();
        activeManager!.ManagerId.Should().Be(secondManager);
    }
}
