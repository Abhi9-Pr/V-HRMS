using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Recruitment;

public readonly record struct InterviewId(Guid Value)
{
    public static InterviewId New() => new(Guid.NewGuid());
}

public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow,
}

public sealed class Interview : AggregateRoot<InterviewId>, ITenantScoped
{
    private readonly List<EmployeeId> _interviewerIds;
    private readonly List<InterviewScorecard> _scorecards = [];

    private Interview(
        InterviewId id, TenantId tenantId, CandidateId candidateId, PipelineStageId pipelineStageId,
        DateTimeOffset scheduledAt, IReadOnlyList<EmployeeId> interviewerIds)
        : base(id)
    {
        TenantId = tenantId;
        CandidateId = candidateId;
        PipelineStageId = pipelineStageId;
        ScheduledAt = scheduledAt;
        _interviewerIds = [.. interviewerIds];
        Status = InterviewStatus.Scheduled;
    }

    public TenantId TenantId { get; }

    public CandidateId CandidateId { get; }

    public PipelineStageId PipelineStageId { get; }

    public DateTimeOffset ScheduledAt { get; private set; }

    public IReadOnlyList<EmployeeId> InterviewerIds => _interviewerIds.AsReadOnly();

    public InterviewStatus Status { get; private set; }

    public string? Feedback { get; private set; }

    public int? Rating { get; private set; }

    public IReadOnlyList<InterviewScorecard> Scorecards => _scorecards.AsReadOnly();

    public static Result<Interview> Schedule(
        TenantId tenantId, CandidateId candidateId, PipelineStageId pipelineStageId, DateTimeOffset scheduledAt,
        IReadOnlyList<EmployeeId> interviewerIds)
    {
        if (interviewerIds is null || interviewerIds.Count == 0)
        {
            return Result.Failure<Interview>(Error.Validation("interview.no_interviewers", "At least one interviewer is required."));
        }

        return Result.Success(new Interview(InterviewId.New(), tenantId, candidateId, pipelineStageId, scheduledAt, interviewerIds));
    }

    public Result Complete(string feedback, int rating)
    {
        if (Status != InterviewStatus.Scheduled)
        {
            return Result.Failure(Error.Conflict("interview.not_scheduled", "Only a scheduled interview can be completed."));
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure(Error.Validation("interview.invalid_rating", "Rating must be between 1 and 5."));
        }

        Status = InterviewStatus.Completed;
        Feedback = feedback;
        Rating = rating;
        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status != InterviewStatus.Scheduled)
        {
            return Result.Failure(Error.Conflict("interview.not_scheduled", "Only a scheduled interview can be cancelled."));
        }

        Status = InterviewStatus.Cancelled;
        return Result.Success();
    }

    public Result Reschedule(DateTimeOffset scheduledAt)
    {
        if (Status != InterviewStatus.Scheduled)
        {
            return Result.Failure(Error.Conflict("interview.not_scheduled", "Only a scheduled interview can be rescheduled."));
        }

        ScheduledAt = scheduledAt;
        return Result.Success();
    }

    public Result SubmitScorecard(EmployeeId interviewerId, int rating, string? notes, DateTimeOffset occurredOn)
    {
        if (!_interviewerIds.Contains(interviewerId))
        {
            return Result.Failure(Error.Conflict("interview.not_an_interviewer", "This person is not an interviewer on this interview."));
        }

        if (rating is < 1 or > 5)
        {
            return Result.Failure(Error.Validation("interview.invalid_rating", "Rating must be between 1 and 5."));
        }

        if (_scorecards.Any(s => s.InterviewerId == interviewerId))
        {
            return Result.Failure(Error.Conflict("interview.scorecard_already_submitted", "This interviewer has already submitted a scorecard."));
        }

        _scorecards.Add(new InterviewScorecard(interviewerId, rating, notes, occurredOn));
        return Result.Success();
    }
}
