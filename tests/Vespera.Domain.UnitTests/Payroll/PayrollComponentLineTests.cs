using FluentAssertions;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollComponentLineTests
{
    [Fact]
    public void Constructor_Should_Reject_A_Negative_Amount()
    {
        var act = () => new PayrollComponentLine(
            SalaryComponentId.New(), "Basic", SalaryComponentType.Earning, PayrollComponentDirection.Earning, Money.Of(-1m, Currency.Inr));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(PayrollComponentDirection.Earning, 40000)]
    [InlineData(PayrollComponentDirection.Deduction, -40000)]
    public void SignedAmount_Should_Flip_Sign_For_Deductions(PayrollComponentDirection direction, decimal expected)
    {
        var line = new PayrollComponentLine(
            SalaryComponentId.New(), "Basic", SalaryComponentType.Earning, direction, Money.Of(40000m, Currency.Inr));

        line.SignedAmount.Should().Be(expected);
    }
}
