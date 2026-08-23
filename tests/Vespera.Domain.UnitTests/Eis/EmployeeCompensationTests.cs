using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Eis;

public class EmployeeCompensationTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void UpdateCompensation_Should_Set_CurrentAnnualCtc()
    {
        var employee = CreateEmployee();
        var ctc = Money.Of(1200000m, Currency.Inr);

        var result = employee.UpdateCompensation(ctc, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.CurrentAnnualCtc.Should().Be(ctc);
    }

    [Fact]
    public void UpdateCompensation_Should_Allow_Clearing_A_Previously_Set_Value()
    {
        var employee = CreateEmployee();
        employee.UpdateCompensation(Money.Of(1200000m, Currency.Inr), Now, "hr@vespera.test");

        var result = employee.UpdateCompensation(null, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        employee.CurrentAnnualCtc.Should().BeNull();
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
