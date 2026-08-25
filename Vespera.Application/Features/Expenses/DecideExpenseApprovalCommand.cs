using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record DecideExpenseApprovalCommand(
    Guid ClaimId, bool Approved, string? Comment, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
