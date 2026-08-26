using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record GetPendingExpenseApprovalsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<ExpenseClaimDto>>>;
