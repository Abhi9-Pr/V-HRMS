using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeCelebrationVisibilityTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_Employee_Should_Default_To_Celebrations_Visible()
    {
        var employee = CreateEmployee();

        employee.CelebrationsVisible.Should().BeTrue();
    }

    [Fact]
    public void SetCelebrationVisibility_Should_Allow_Opting_Out()
    {
        var employee = CreateEmployee();

        var result = employee.SetCelebrationVisibility(false, Now, "employee@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.CelebrationsVisible.Should().BeFalse();
    }

    [Fact]
    public void SetCelebrationVisibility_Should_Allow_Opting_Back_In()
    {
        var employee = CreateEmployee();
        employee.SetCelebrationVisibility(false, Now, "employee@vespera.test");

        employee.SetCelebrationVisibility(true, Now, "employee@vespera.test");

        employee.CelebrationsVisible.Should().BeTrue();
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
