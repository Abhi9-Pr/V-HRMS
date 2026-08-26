using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record ScheduleInterviewCommand(
    Guid CandidateId, Guid PipelineStageId, DateTimeOffset ScheduledAt, IReadOnlyList<Guid> InterviewerIds, string? IdempotencyKey)
    : IRequest<Result<Guid>>, IIdempotentRequest;
