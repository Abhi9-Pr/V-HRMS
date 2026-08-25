using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

public sealed record OpenExpenseClaimCommand(Currency SettlementCurrency, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
