using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class SubmitExpenseClaimCommandHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IReadRepository<ExpensePolicy> _expensePolicies = Substitute.For<IReadRepository<ExpensePolicy>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly IWriteRepository<ApprovalChain> _approvalChains = Substitute.For<IWriteRepository<ApprovalChain>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private readonly EmployeeId _managerId = EmployeeId.New();

    public SubmitExpenseClaimCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(BuildEmployee());
        _expensePolicies.ListAsync(Arg.Any<ExpensePoliciesByCategoriesSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private Employee BuildEmployee()
    {
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-100").Value, "Test", "Employee",
            EmailAddress.Create("test.employee@demo.test").Value, PhoneNumber.Create("+919812345000").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "system").Value;

        // Reflection-free: EmployeeId is a private ctor param, but the handler only reads
        // claim.EmployeeId (already fixed) and compares employee.DesignationId — Onboard's
        // generated Id doesn't need to match _employeeId for this test's assertions.
        return employee;
    }

    private ExpenseClaim CreateOwnedSubmittableClaim()
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        return claim;
    }

    private SubmitExpenseClaimCommandHandler CreateHandler(IEnumerable<IExpensePolicyRule>? rules = null) => new(
        _expenseClaims, _expensePolicies, _employees, _reportingRelationships, _proxyDelegations, _approvalChains,
        new ExpensePolicyEvaluator(rules ?? []), _tenantContext, _dateTimeProvider, _notificationDispatcher,
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    [Fact]
    public async Task Handle_Should_Submit_The_Claim_Open_An_Approval_Chain_And_Notify_The_Approver()
    {
        var claim = CreateOwnedSubmittableClaim();
        var relationship = ReportingRelationship.Create(_tenantId, _employeeId, _managerId, new DateOnly(2020, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveReportingRelationshipByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitExpenseClaimCommand(claim.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Submitted);
        await _approvalChains.Received(1).AddAsync(
            Arg.Is<ApprovalChain>(c => c.SubjectType == ApprovalSubjectType.ExpenseClaim && c.CurrentStep.ApproverId == _managerId),
            Arg.Any<CancellationToken>());
        await _notificationDispatcher.Received(1).DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_A_Blocking_Policy_Is_Violated()
    {
        var claim = CreateOwnedSubmittableClaim();
        var policy = ExpensePolicy.Create(
            _tenantId, "Travel", Money.Of(100m, Currency.Inr), Money.Of(0m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;
        _expensePolicies.ListAsync(Arg.Any<ExpensePoliciesByCategoriesSpecification>(), Arg.Any<CancellationToken>())
            .Returns([policy]);

        var handler = CreateHandler([new MaxAmountPerClaimRule()]);
        var result = await handler.Handle(new SubmitExpenseClaimCommand(claim.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Draft);
        await _approvalChains.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Active_Manager_Is_Configured()
    {
        var claim = CreateOwnedSubmittableClaim();
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveReportingRelationshipByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new SubmitExpenseClaimCommand(claim.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _approvalChains.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
