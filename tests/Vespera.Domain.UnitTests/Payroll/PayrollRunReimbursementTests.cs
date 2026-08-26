using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollRunReimbursementTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddReimbursement_Should_Succeed_While_Draft()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026, Now, "seed").Value;
        var claimId = Guid.NewGuid();

        var result = run.AddReimbursement(EmployeeId.New(), Money.Of(1500m, Currency.Inr), claimId);

        result.IsSuccess.Should().BeTrue();
        run.Reimbursements.Should().ContainSingle(r => r.SourceExpenseClaimId == claimId && r.Amount == Money.Of(1500m, Currency.Inr));
    }

    [Fact]
    public void AddReimbursement_Should_Fail_Once_The_Run_Is_No_Longer_Draft()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026, Now, "seed").Value;
        run.FreezeAttendance(DateOnly.FromDateTime(Now.UtcDateTime), freezeDay: 1, Now, "seed");

        var result = run.AddReimbursement(EmployeeId.New(), Money.Of(1500m, Currency.Inr), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddReimbursement_Should_Fail_When_The_Same_Claim_Is_Added_Twice()
    {
        var run = PayrollRun.Open(TenantId.New(), 1, 2026, Now, "seed").Value;
        var claimId = Guid.NewGuid();
        run.AddReimbursement(EmployeeId.New(), Money.Of(1500m, Currency.Inr), claimId);

        var result = run.AddReimbursement(EmployeeId.New(), Money.Of(1500m, Currency.Inr), claimId);

        result.IsFailure.Should().BeTrue();
    }
}
