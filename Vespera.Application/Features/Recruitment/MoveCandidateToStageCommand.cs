using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record MoveCandidateToStageCommand(
    Guid CandidateId, Guid TargetStageId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
