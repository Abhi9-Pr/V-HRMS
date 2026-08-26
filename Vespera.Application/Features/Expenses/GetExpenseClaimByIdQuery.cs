using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record GetExpenseClaimByIdQuery(Guid Id) : IRequest<Result<ExpenseClaimDto>>;
