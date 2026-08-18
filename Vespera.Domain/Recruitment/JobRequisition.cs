using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Recruitment;

public readonly record struct PipelineStageId(Guid Value)
{
    public static PipelineStageId New() => new(Guid.NewGuid());
}

public sealed class PipelineStage : Entity<PipelineStageId>
{
    internal PipelineStage(PipelineStageId id, string name, int sequenceNumber)
        : base(id)
    {
        Name = name;
        SequenceNumber = sequenceNumber;
    }

    public string Name { get; }

    public int SequenceNumber { get; }
}

public readonly record struct JobRequisitionId(Guid Value)
{
    public static JobRequisitionId New() => new(Guid.NewGuid());
}

public enum JobRequisitionStatus
{
    Open,
    OnHold,
    Closed,
    Filled,
}

public sealed class JobRequisition : AuditableTenantAggregateRoot<JobRequisitionId>
{
    private readonly List<PipelineStage> _stages = [];

    private JobRequisition(
        JobRequisitionId id, TenantId tenantId, string title, DepartmentId departmentId, int openingsCount,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Title = title;
        DepartmentId = departmentId;
        OpeningsCount = openingsCount;
        Status = JobRequisitionStatus.Open;
    }

    public string Title { get; private set; }

    public DepartmentId DepartmentId { get; }

    public int OpeningsCount { get; private set; }

    public JobRequisitionStatus Status { get; private set; }

    public IReadOnlyList<PipelineStage> Stages => _stages.AsReadOnly();

    public static Result<JobRequisition> Create(
        TenantId tenantId, string title, DepartmentId departmentId, int openingsCount, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<JobRequisition>(Error.Validation("job_requisition.title_required", "Title is required."));
        }

        if (openingsCount <= 0)
        {
            return Result.Failure<JobRequisition>(Error.Validation("job_requisition.invalid_openings", "Openings count must be positive."));
        }

        return Result.Success(new JobRequisition(JobRequisitionId.New(), tenantId, title.Trim(), departmentId, openingsCount, occurredOn, createdBy));
    }

    public Result AddStage(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("job_requisition.stage_name_required", "Stage name is required."));
        }

        _stages.Add(new PipelineStage(PipelineStageId.New(), name.Trim(), _stages.Count));
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Close(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == JobRequisitionStatus.Closed)
        {
            return Result.Failure(Error.Conflict("job_requisition.already_closed", "Requisition is already closed."));
        }

        Status = JobRequisitionStatus.Closed;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result PutOnHold(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status != JobRequisitionStatus.Open)
        {
            return Result.Failure(Error.Conflict("job_requisition.not_open", "Only an open requisition can be put on hold."));
        }

        Status = JobRequisitionStatus.OnHold;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reopen(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status != JobRequisitionStatus.OnHold)
        {
            return Result.Failure(Error.Conflict("job_requisition.not_on_hold", "Only a requisition on hold can be reopened."));
        }

        Status = JobRequisitionStatus.Open;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
