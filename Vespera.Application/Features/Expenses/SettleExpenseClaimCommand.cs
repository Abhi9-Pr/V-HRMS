using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record SettleExpenseClaimCommand(Guid ExpenseClaimId, Guid PayrollRunId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
