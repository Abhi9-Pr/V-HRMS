using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record SubmitExpenseClaimCommand(Guid ClaimId, string? IdempotencyKey) : IRequest<Result<SubmitExpenseClaimResultDto>>, IIdempotentRequest;

public sealed record SubmitExpenseClaimResultDto(IReadOnlyList<string> Warnings);
