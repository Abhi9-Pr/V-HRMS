using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmploymentHistoryTimelineTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AsOf_Should_Return_Null_For_An_Empty_History()
    {
        EmploymentHistoryTimeline.AsOf(Array.Empty<EmploymentHistory>(), new DateOnly(2026, 1, 1)).Should().BeNull();
    }

    [Fact]
    public void AsOf_Should_Return_The_Single_Hire_Entry_When_Only_One_Exists()
    {
        var employee = CreateEmployee(new DateOnly(2024, 1, 15));

        var result = EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, new DateOnly(2026, 1, 1));

        result.Should().NotBeNull();
        result!.ChangeReason.Should().Be(EmploymentChangeReason.Hire);
    }

    [Fact]
    public void AsOf_Should_Return_Null_For_A_Date_Before_The_Hire_Date()
    {
        var employee = CreateEmployee(new DateOnly(2024, 1, 15));

        var result = EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, new DateOnly(2023, 1, 1));

        result.Should().BeNull();
    }

    [Fact]
    public void AsOf_Should_Return_The_Latest_Entry_Effective_On_Or_Before_The_Given_Date()
    {
        var employee = CreateEmployee(new DateOnly(2024, 1, 15));
        var secondDepartment = DepartmentId.New();
        employee.Transfer(
            secondDepartment, DesignationId.New(), LocationId.New(),
            new DateOnly(2025, 6, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");
        var thirdDepartment = DepartmentId.New();
        employee.Transfer(
            thirdDepartment, DesignationId.New(), LocationId.New(),
            new DateOnly(2026, 3, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");

        var result = EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, new DateOnly(2025, 12, 1));

        result.Should().NotBeNull();
        result!.DepartmentId.Should().Be(secondDepartment);
    }

    [Fact]
    public void AsOf_Should_Match_An_Entry_Whose_EffectiveFrom_Equals_The_Given_Date_Exactly()
    {
        var employee = CreateEmployee(new DateOnly(2024, 1, 15));

        var result = EmploymentHistoryTimeline.AsOf(employee.EmploymentHistory, new DateOnly(2024, 1, 15));

        result.Should().NotBeNull();
        result!.ChangeReason.Should().Be(EmploymentChangeReason.Hire);
    }

    private static Employee CreateEmployee(DateOnly dateOfJoining)
    {
        var code = EmployeeCode.Create("EMP-100").Value;
        var email = EmailAddress.Create("employee@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        return Employee.Onboard(
            TenantId, code, "Ada", "Lovelace", email, phone,
            new DateOnly(1990, 1, 1), dateOfJoining,
            DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "hr@vespera.test").Value;
    }
}
