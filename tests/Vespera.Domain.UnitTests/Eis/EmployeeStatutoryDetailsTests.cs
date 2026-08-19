using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeStatutoryDetailsTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateStatutoryDetails_Should_Set_Pan_And_BankAccount()
    {
        var employee = CreateEmployee();
        var pan = PanNumber.Create("ABCDE1234F").Value;
        var bankAccount = BankAccountNumber.Create("123456789012").Value;

        var result = employee.UpdateStatutoryDetails(pan, bankAccount, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Pan.Should().Be(pan);
        employee.BankAccount.Should().Be(bankAccount);
    }

    [Fact]
    public void UpdateStatutoryDetails_Should_Allow_Clearing_Previously_Set_Values()
    {
        var employee = CreateEmployee();
        var pan = PanNumber.Create("ABCDE1234F").Value;
        employee.UpdateStatutoryDetails(pan, null, Now, "hr@vespera.test");

        var result = employee.UpdateStatutoryDetails(null, null, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.Pan.Should().BeNull();
        employee.BankAccount.Should().BeNull();
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
