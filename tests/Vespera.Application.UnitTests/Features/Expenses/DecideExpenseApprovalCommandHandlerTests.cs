using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class DecideExpenseApprovalCommandHandlerTests
{
    private readonly IReadRepository<ApprovalChain> _approvalChains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private readonly EmployeeId _approverId = EmployeeId.New();

    public DecideExpenseApprovalCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private (ApprovalChain Chain, ExpenseClaim Claim) CreateSubmittedClaimWithChain()
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        claim.Submit();

        var chain = ApprovalChain.Create(_tenantId, ApprovalSubjectType.ExpenseClaim, claim.Id.Value, [_approverId]).Value;

        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        _approvalChains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);

        return (chain, claim);
    }

    private DecideExpenseApprovalCommandHandler CreateHandler(EmployeeId signedInAs) => new(
        _approvalChains, _expenseClaims, _proxyDelegations, _tenantContext, _dateTimeProvider, _notificationDispatcher,
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, signedInAs));

    [Fact]
    public async Task Handle_Should_Approve_The_Claim_When_The_Current_Approver_Approves()
    {
        var (_, claim) = CreateSubmittedClaimWithChain();
        var handler = CreateHandler(_approverId);

        var result = await handler.Handle(new DecideExpenseApprovalCommand(claim.Id.Value, true, "Looks good", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Approved);
        await _notificationDispatcher.Received(1).DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Reject_The_Claim_When_The_Current_Approver_Rejects()
    {
        var (_, claim) = CreateSubmittedClaimWithChain();
        var handler = CreateHandler(_approverId);

        var result = await handler.Handle(
            new DecideExpenseApprovalCommand(claim.Id.Value, false, "Not a valid expense", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Rejected);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Current_Approver()
    {
        var (_, claim) = CreateSubmittedClaimWithChain();
        var handler = CreateHandler(EmployeeId.New());

        var result = await handler.Handle(new DecideExpenseApprovalCommand(claim.Id.Value, true, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Submitted);
    }
}
