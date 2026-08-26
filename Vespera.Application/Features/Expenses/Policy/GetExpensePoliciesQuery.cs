using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed record GetExpensePoliciesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<ExpensePolicyDto>>>;

public sealed record ExpensePolicyDto(
    Guid Id,
    string Category,
    decimal MaxAmountPerClaim,
    decimal ReceiptRequiredAboveAmount,
    Currency Currency,
    Guid? ApplicableDesignationId,
    ExpensePolicySeverity MaxAmountSeverity,
    ExpensePolicySeverity ReceiptRequiredSeverity);
