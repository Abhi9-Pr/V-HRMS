using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Expenses;

/// <summary>Runs the category policy engine, transitions the claim to Submitted, and opens a
/// single-step <see cref="ApprovalChain"/> against the claimant's resolved manager — reusing the
/// same generic approval-chain engine Leave uses (<see cref="ApprovalSubjectType.ExpenseClaim"/>).</summary>
public sealed class SubmitExpenseClaimCommandHandler : IRequestHandler<SubmitExpenseClaimCommand, Result<SubmitExpenseClaimResultDto>>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IReadRepository<ExpensePolicy> _expensePolicies;
    private readonly IReadRepository<Employee> _employees;
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly IWriteRepository<ApprovalChain> _approvalChains;
    private readonly ExpensePolicyEvaluator _policyEvaluator;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public SubmitExpenseClaimCommandHandler(
        IReadRepository<ExpenseClaim> expenseClaims,
        IReadRepository<ExpensePolicy> expensePolicies,
        IReadRepository<Employee> employees,
        IReadRepository<ReportingRelationship> reportingRelationships,
        IReadRepository<ProxyDelegation> proxyDelegations,
        IWriteRepository<ApprovalChain> approvalChains,
        ExpensePolicyEvaluator policyEvaluator,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        INotificationDispatcher notificationDispatcher,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _expensePolicies = expensePolicies;
        _employees = employees;
        _reportingRelationships = reportingRelationships;
        _proxyDelegations = proxyDelegations;
        _approvalChains = approvalChains;
        _policyEvaluator = policyEvaluator;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _notificationDispatcher = notificationDispatcher;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<SubmitExpenseClaimResultDto>> Handle(SubmitExpenseClaimCommand request, CancellationToken cancellationToken)
    {
        var claim = await _expenseClaims.FirstOrDefaultAsync(
            new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.ClaimId)), cancellationToken);

        if (claim is null)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null || claim.EmployeeId != currentEmployeeId.Value)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(claim.EmployeeId), cancellationToken);
        if (employee is null)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(Error.NotFound("expense_claim.employee_not_found", "Claimant not found."));
        }

        var categories = claim.Lines.Select(line => line.Category).Distinct().ToList();
        var policies = await _expensePolicies.ListAsync(
            new ExpensePoliciesByCategoriesSpecification(_tenantContext.TenantId, categories), cancellationToken);
        var applicablePolicies = policies
            .Where(policy => policy.ApplicableDesignationId is null || policy.ApplicableDesignationId == employee.DesignationId)
            .ToList();

        var violations = _policyEvaluator.Evaluate(claim, applicablePolicies);
        var blockingViolations = violations.Where(v => v.Severity == ExpensePolicySeverity.Block).ToList();
        if (blockingViolations.Count > 0)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(Error.Validation(
                "expense_claim.policy_violation", string.Join(" ", blockingViolations.Select(v => v.Message))));
        }

        var submitResult = claim.Submit();
        if (submitResult.IsFailure)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(submitResult.Error);
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var manager = await _reportingRelationships.FirstOrDefaultAsync(
            new ActiveReportingRelationshipByEmployeeSpecification(_tenantContext.TenantId, claim.EmployeeId, today), cancellationToken);

        if (manager is null)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(
                Error.Validation("expense_claim.no_approver", "No active manager is configured for this employee."));
        }

        var delegations = await _proxyDelegations.ListAsync(
            new ProxyDelegationsByDelegatorSpecification(_tenantContext.TenantId, manager.ManagerId), cancellationToken);
        var resolvedApproverId = new ApprovalChainResolver().ResolveApprover(manager.ManagerId, today, delegations);

        var chainResult = ApprovalChain.Create(
            _tenantContext.TenantId, ApprovalSubjectType.ExpenseClaim, claim.Id.Value, [resolvedApproverId], _dateTimeProvider.UtcNow);
        if (chainResult.IsFailure)
        {
            return Result.Failure<SubmitExpenseClaimResultDto>(chainResult.Error);
        }

        await _approvalChains.AddAsync(chainResult.Value, cancellationToken);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                resolvedApproverId.Value.ToString(),
                "Expense claim awaiting your approval",
                $"An expense claim totalling {claim.Total(claim.SettlementCurrency)} is awaiting your approval.",
                new Dictionary<string, string> { ["expenseClaimId"] = claim.Id.Value.ToString() }),
            cancellationToken);

        var warnings = violations.Where(v => v.Severity == ExpensePolicySeverity.Warn).Select(v => v.Message).ToList();
        return Result.Success(new SubmitExpenseClaimResultDto(warnings));
    }
}
