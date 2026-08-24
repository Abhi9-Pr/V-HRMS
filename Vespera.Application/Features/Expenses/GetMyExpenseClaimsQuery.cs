using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record GetMyExpenseClaimsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<ExpenseClaimDto>>>;
