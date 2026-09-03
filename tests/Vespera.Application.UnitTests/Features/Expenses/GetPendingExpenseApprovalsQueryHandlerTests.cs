using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class GetPendingExpenseApprovalsQueryHandlerTests
{
    private readonly IReadRepository<ApprovalChain> _approvalChains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _approverId = EmployeeId.New();

    public GetPendingExpenseApprovalsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private GetPendingExpenseApprovalsQueryHandler CreateHandler() => new(
        _approvalChains, _expenseClaims, _proxyDelegations, _tenantContext, _dateTimeProvider,
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, _approverId));

    [Fact]
    public async Task Handle_Should_Return_Claims_Awaiting_The_Current_Employees_Approval()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        claim.Submit();

        var chain = ApprovalChain.Create(
            _tenantId, ApprovalSubjectType.ExpenseClaim, claim.Id.Value, [_approverId], _dateTimeProvider.UtcNow).Value;

        _approvalChains.ListAsync(Arg.Any<PendingApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _expenseClaims.ListAsync(Arg.Any<ExpenseClaimsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([claim]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPendingExpenseApprovalsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == claim.Id.Value);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Exclude_Claims_Awaiting_A_Different_Approver()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        claim.Submit();

        var chain = ApprovalChain.Create(
            _tenantId, ApprovalSubjectType.ExpenseClaim, claim.Id.Value, [EmployeeId.New()], _dateTimeProvider.UtcNow).Value;

        _approvalChains.ListAsync(Arg.Any<PendingApprovalChainsSpecification>(), Arg.Any<CancellationToken>()).Returns([chain]);
        _expenseClaims.ListAsync(Arg.Any<ExpenseClaimsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([claim]);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetPendingExpenseApprovalsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_Account_Has_No_Linked_Employee()
    {
        var handler = new GetPendingExpenseApprovalsQueryHandler(
            _approvalChains, _expenseClaims, _proxyDelegations, _tenantContext, _dateTimeProvider,
            new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), Substitute.For<ICurrentUser>()));

        var result = await handler.Handle(new GetPendingExpenseApprovalsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.no_employee");
    }
}
