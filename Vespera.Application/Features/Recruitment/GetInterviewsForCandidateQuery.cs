using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetInterviewsForCandidateQuery(Guid CandidateId) : IRequest<Result<IReadOnlyList<InterviewDto>>>;

public sealed record InterviewScorecardDto(Guid InterviewerId, int Rating, string? Notes, DateTimeOffset SubmittedAt);

public sealed record InterviewDto(
    Guid Id, Guid CandidateId, Guid PipelineStageId, DateTimeOffset ScheduledAt, IReadOnlyList<Guid> InterviewerIds,
    InterviewStatus Status, string? Feedback, int? Rating, IReadOnlyList<InterviewScorecardDto> Scorecards);
