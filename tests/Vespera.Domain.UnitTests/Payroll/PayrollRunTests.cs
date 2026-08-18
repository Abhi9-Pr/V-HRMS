using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollRunTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Finalize_Should_Fail_When_There_Are_No_Lines()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026).Value;

        var result = run.Finalize(Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Finalize_Should_Raise_PayrollFinalized()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026).Value;
        run.AddLine(EmployeeId.New(), Money.Of(50000m, Currency.Inr), Money.Of(5000m, Currency.Inr), Money.Of(45000m, Currency.Inr), 0m);

        var result = run.Finalize(Now);

        result.IsSuccess.Should().BeTrue();
        run.DomainEvents.Should().ContainSingle(e => e is PayrollFinalized);
    }

    [Fact]
    public void Finalize_Should_Fail_When_The_Run_Is_Already_Finalized()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026).Value;
        run.AddLine(EmployeeId.New(), Money.Of(50000m, Currency.Inr), Money.Of(5000m, Currency.Inr), Money.Of(45000m, Currency.Inr), 0m);
        run.Finalize(Now);

        var result = run.Finalize(Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddLine_Should_Fail_Once_The_Run_Is_No_Longer_In_Draft()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026).Value;
        run.AddLine(EmployeeId.New(), Money.Of(50000m, Currency.Inr), Money.Of(5000m, Currency.Inr), Money.Of(45000m, Currency.Inr), 0m);
        run.Finalize(Now);

        var result = run.AddLine(EmployeeId.New(), Money.Of(1m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(1m, Currency.Inr), 0m);

        result.IsFailure.Should().BeTrue();
    }
}
