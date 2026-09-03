using FluentAssertions;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollLineTests
{
    [Fact]
    public void Constructor_Should_Expose_Every_Field()
    {
        var employeeId = EmployeeId.New();
        var gross = Money.Of(50000m, Currency.Inr);
        var deductions = Money.Of(5000m, Currency.Inr);
        var net = Money.Of(45000m, Currency.Inr);

        var line = new PayrollLine(PayrollLineId.New(), employeeId, gross, deductions, net, 1.5m);

        line.EmployeeId.Should().Be(employeeId);
        line.Gross.Should().Be(gross);
        line.Deductions.Should().Be(deductions);
        line.Net.Should().Be(net);
        line.LossOfPayDays.Should().Be(1.5m);
    }

    [Fact]
    public void New_Ids_Should_Be_Distinct()
    {
        PayrollLineId.New().Should().NotBe(PayrollLineId.New());
    }
}
