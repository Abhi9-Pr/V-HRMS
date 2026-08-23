using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeBiometricMappingTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssignBiometricDeviceUserId_Should_Set_The_Value()
    {
        var employee = CreateEmployee();

        var result = employee.AssignBiometricDeviceUserId("ZK-042", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.BiometricDeviceUserId.Should().Be("ZK-042");
    }

    [Fact]
    public void AssignBiometricDeviceUserId_Should_Trim_Whitespace()
    {
        var employee = CreateEmployee();

        employee.AssignBiometricDeviceUserId("  ZK-042  ", Now, "hr@vespera.test");

        employee.BiometricDeviceUserId.Should().Be("ZK-042");
    }

    [Fact]
    public void AssignBiometricDeviceUserId_Should_Allow_Clearing_A_Previously_Set_Value()
    {
        var employee = CreateEmployee();
        employee.AssignBiometricDeviceUserId("ZK-042", Now, "hr@vespera.test");

        var result = employee.AssignBiometricDeviceUserId(null, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.BiometricDeviceUserId.Should().BeNull();
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
