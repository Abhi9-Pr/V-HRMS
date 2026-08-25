using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record DecideRequisitionApprovalCommand(
    Guid RequisitionId, bool Approved, string? Comment, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
