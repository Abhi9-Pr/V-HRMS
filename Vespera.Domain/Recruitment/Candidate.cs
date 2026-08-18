using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Recruitment;

public readonly record struct CandidateId(Guid Value)
{
    public static CandidateId New() => new(Guid.NewGuid());
}

public enum CandidateStatus
{
    New,
    InPipeline,
    Offered,
    Hired,
    Rejected,
    Withdrawn,
}

public sealed class Candidate : AggregateRoot<CandidateId>, ITenantScoped
{
    private Candidate(CandidateId id, TenantId tenantId, string fullName, EmailAddress email, PhoneNumber phone)
        : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
        Phone = phone;
        Status = CandidateStatus.New;
    }

    public TenantId TenantId { get; }

    public string FullName { get; }

    public EmailAddress Email { get; }

    public PhoneNumber Phone { get; }

    public CandidateStatus Status { get; private set; }

    public PipelineStageId? CurrentPipelineStageId { get; private set; }

    public static Result<Candidate> Create(TenantId tenantId, string fullName, EmailAddress email, PhoneNumber phone)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<Candidate>(Error.Validation("candidate.full_name_required", "Full name is required."));
        }

        return Result.Success(new Candidate(CandidateId.New(), tenantId, fullName.Trim(), email, phone));
    }

    public Result MoveToStage(PipelineStageId stageId)
    {
        if (!IsActive())
        {
            return Result.Failure(Error.Conflict("candidate.not_active", "Cannot move a closed-out candidate to a new stage."));
        }

        CurrentPipelineStageId = stageId;
        Status = CandidateStatus.InPipeline;
        return Result.Success();
    }

    public Result MarkOffered()
    {
        if (!IsActive())
        {
            return Result.Failure(Error.Conflict("candidate.not_active", "Cannot offer a closed-out candidate."));
        }

        Status = CandidateStatus.Offered;
        return Result.Success();
    }

    public Result MarkHired()
    {
        if (!IsActive())
        {
            return Result.Failure(Error.Conflict("candidate.not_active", "Cannot hire a closed-out candidate."));
        }

        Status = CandidateStatus.Hired;
        return Result.Success();
    }

    public Result Reject()
    {
        if (!IsActive())
        {
            return Result.Failure(Error.Conflict("candidate.not_active", "Cannot reject a closed-out candidate."));
        }

        Status = CandidateStatus.Rejected;
        return Result.Success();
    }

    public Result Withdraw()
    {
        if (!IsActive())
        {
            return Result.Failure(Error.Conflict("candidate.not_active", "Cannot withdraw a closed-out candidate."));
        }

        Status = CandidateStatus.Withdrawn;
        return Result.Success();
    }

    private bool IsActive() => Status is not (CandidateStatus.Hired or CandidateStatus.Rejected or CandidateStatus.Withdrawn);
}
