using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

/// <summary>Schedules the interview, then notifies every interviewer and the candidate with a
/// hand-generated ICS invite in the notification's Metadata (see IcsInviteGenerator) — this
/// codebase has no email-attachment concept, so it's delivered as text, not a real .ics MIME
/// part.</summary>
public sealed class ScheduleInterviewCommandHandler : IRequestHandler<ScheduleInterviewCommand, Result<Guid>>
{
    private const int DurationMinutes = 60;

    private readonly IWriteRepository<Interview> _interviews;
    private readonly IReadRepository<Candidate> _candidates;
    private readonly ITenantContext _tenantContext;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ScheduleInterviewCommandHandler(
        IWriteRepository<Interview> interviews, IReadRepository<Candidate> candidates, ITenantContext tenantContext,
        INotificationDispatcher notificationDispatcher)
    {
        _interviews = interviews;
        _candidates = candidates;
        _tenantContext = tenantContext;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<Guid>> Handle(ScheduleInterviewCommand request, CancellationToken cancellationToken)
    {
        var candidate = await _candidates.FirstOrDefaultAsync(
            new CandidateByIdSpecification(new CandidateId(request.CandidateId)), cancellationToken);

        if (candidate is null)
        {
            return Result.Failure<Guid>(Error.NotFound("candidate.not_found", "Candidate not found."));
        }

        var interviewerIds = request.InterviewerIds.Select(id => new EmployeeId(id)).ToList();

        var result = Interview.Schedule(
            _tenantContext.TenantId, candidate.Id, new PipelineStageId(request.PipelineStageId), request.ScheduledAt, interviewerIds);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _interviews.AddAsync(result.Value, cancellationToken);

        var icsContent = IcsInviteGenerator.Generate(
            $"Interview: {candidate.FullName}", request.ScheduledAt, TimeSpan.FromMinutes(DurationMinutes), "recruiting@vespera.test", []);

        foreach (var interviewerId in interviewerIds)
        {
            await _notificationDispatcher.DispatchAsync(
                new NotificationMessage(
                    interviewerId.Value.ToString(),
                    "Interview scheduled",
                    $"You are scheduled to interview {candidate.FullName} at {request.ScheduledAt}.",
                    new Dictionary<string, string> { ["icsContent"] = icsContent, ["interviewId"] = result.Value.Id.Value.ToString() }),
                cancellationToken);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                candidate.Id.Value.ToString(),
                "Interview scheduled",
                $"Your interview is scheduled for {request.ScheduledAt}.",
                new Dictionary<string, string> { ["icsContent"] = icsContent, ["interviewId"] = result.Value.Id.Value.ToString() }),
            cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
