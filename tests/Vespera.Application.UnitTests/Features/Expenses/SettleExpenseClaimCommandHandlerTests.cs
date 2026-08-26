using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class SettleExpenseClaimCommandHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly TenantId _tenantId = TenantId.New();

    private SettleExpenseClaimCommandHandler CreateHandler() => new(_expenseClaims, _payrollRuns);

    private ExpenseClaim CreateApprovedClaim()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        claim.Submit();
        claim.Approve();
        return claim;
    }

    [Fact]
    public async Task Handle_Should_Add_A_Reimbursement_Line_And_Mark_The_Claim_Reimbursed()
    {
        var claim = CreateApprovedClaim();
        var payrollRun = PayrollRun.Open(_tenantId, 1, 2026, DateTimeOffset.UtcNow, "system").Value;

        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var handler = CreateHandler();
        var result = await handler.Handle(new SettleExpenseClaimCommand(claim.Id.Value, payrollRun.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Reimbursed);
        payrollRun.Reimbursements.Should().ContainSingle(
            r => r.SourceExpenseClaimId == claim.Id.Value && r.Amount == Money.Of(1500m, Currency.Inr));
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Claim_Is_Not_Approved()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        var payrollRun = PayrollRun.Open(_tenantId, 1, 2026, DateTimeOffset.UtcNow, "system").Value;

        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var handler = CreateHandler();
        var result = await handler.Handle(new SettleExpenseClaimCommand(claim.Id.Value, payrollRun.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Payroll_Run_Is_Not_Draft()
    {
        var claim = CreateApprovedClaim();
        var payrollRun = PayrollRun.Open(_tenantId, 1, 2026, DateTimeOffset.UtcNow, "system").Value;
        payrollRun.FreezeAttendance(DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime), freezeDay: 1, DateTimeOffset.UtcNow, "system");

        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var handler = CreateHandler();
        var result = await handler.Handle(new SettleExpenseClaimCommand(claim.Id.Value, payrollRun.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
