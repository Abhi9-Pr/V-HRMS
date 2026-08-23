using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeePersonalDetailsTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdatePersonalDetails_Should_Set_Name_Email_And_Phone()
    {
        var employee = CreateEmployee();
        var newEmail = EmailAddress.Create("ada.lovelace@vespera.test").Value;
        var newPhone = PhoneNumber.Create("+442071234567").Value;

        var result = employee.UpdatePersonalDetails("Augusta", "King", newEmail, newPhone, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.FirstName.Should().Be("Augusta");
        employee.LastName.Should().Be("King");
        employee.WorkEmail.Should().Be(newEmail);
        employee.Phone.Should().Be(newPhone);
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Fail_When_FirstName_Is_Blank()
    {
        var employee = CreateEmployee();

        var result = employee.UpdatePersonalDetails(
            "   ", "King", employee.WorkEmail, employee.Phone, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.first_name_required");
    }

    [Fact]
    public void UpdatePersonalDetails_Should_Fail_When_LastName_Is_Blank()
    {
        var employee = CreateEmployee();

        var result = employee.UpdatePersonalDetails(
            "Augusta", "   ", employee.WorkEmail, employee.Phone, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.last_name_required");
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
