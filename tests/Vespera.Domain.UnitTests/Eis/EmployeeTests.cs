using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Eis.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Onboard_Should_Raise_EmployeeOnboarded_And_Seed_EmploymentHistory()
    {
        var employee = CreateEmployee();

        employee.DomainEvents.Should().ContainSingle(e => e is EmployeeOnboarded);
        employee.EmploymentHistory.Should().ContainSingle(h => h.ChangeReason == EmploymentChangeReason.Hire);
        employee.Status.Should().Be(EmploymentStatus.Active);
    }

    [Fact]
    public void Transfer_Should_Update_Assignment_And_Append_History()
    {
        var employee = CreateEmployee();
        var newDepartment = DepartmentId.New();
        var newDesignation = DesignationId.New();
        var newLocation = LocationId.New();

        var result = employee.Transfer(
            newDepartment, newDesignation, newLocation, new DateOnly(2026, 3, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.DepartmentId.Should().Be(newDepartment);
        employee.EmploymentHistory.Should().HaveCount(2);
    }

    [Fact]
    public void Exit_Should_Raise_EmployeeExited_And_Set_Status()
    {
        var employee = CreateEmployee();

        var result = employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Status.Should().Be(EmploymentStatus.Exited);
        employee.DomainEvents.Should().ContainSingle(e => e is EmployeeExited);
    }

    [Fact]
    public void Exit_Should_Fail_When_Employee_Already_Exited()
    {
        var employee = CreateEmployee();
        employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        var result = employee.Exit(new DateOnly(2026, 7, 1), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Transfer_Should_Fail_For_An_Exited_Employee()
    {
        var employee = CreateEmployee();
        employee.Exit(new DateOnly(2026, 6, 30), EmployeeExitReason.Resignation, Now, "hr@vespera.test");

        var result = employee.Transfer(
            DepartmentId.New(), DesignationId.New(), LocationId.New(), new DateOnly(2026, 7, 1), EmploymentChangeReason.Transfer, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    private static Employee CreateEmployee()
    {
        var code = EmployeeCode.Create("EMP-100").Value;
        var email = EmailAddress.Create("employee@vespera.test").Value;
        var phone = PhoneNumber.Create("+14155552671").Value;

        return Employee.Onboard(
            TenantId, code, "Ada", "Lovelace", email, phone,
            new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(),
            Now, "hr@vespera.test").Value;
    }
}
