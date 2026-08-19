using Vespera.Domain.Common;

namespace Vespera.Domain.Helpdesk;

public readonly record struct SlaPolicyId(Guid Value)
{
    public static SlaPolicyId New() => new(Guid.NewGuid());
}

public sealed class SlaPolicy : AuditableTenantAggregateRoot<SlaPolicyId>
{
    private SlaPolicy(
        SlaPolicyId id, TenantId tenantId, string name, TimeSpan responseTime, TimeSpan resolutionTime,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        ResponseTime = responseTime;
        ResolutionTime = resolutionTime;
    }

    public string Name { get; private set; }

    public TimeSpan ResponseTime { get; private set; }

    public TimeSpan ResolutionTime { get; private set; }

    public static Result<SlaPolicy> Create(
        TenantId tenantId, string name, TimeSpan responseTime, TimeSpan resolutionTime, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<SlaPolicy>(Error.Validation("sla_policy.name_required", "SLA policy name is required."));
        }

        if (resolutionTime < responseTime)
        {
            return Result.Failure<SlaPolicy>(Error.Validation("sla_policy.invalid_targets", "Resolution time cannot be shorter than response time."));
        }

        return Result.Success(new SlaPolicy(SlaPolicyId.New(), tenantId, name.Trim(), responseTime, resolutionTime, occurredOn, createdBy));
    }

    public Result UpdateTargets(TimeSpan responseTime, TimeSpan resolutionTime, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (resolutionTime < responseTime)
        {
            return Result.Failure(Error.Validation("sla_policy.invalid_targets", "Resolution time cannot be shorter than response time."));
        }

        ResponseTime = responseTime;
        ResolutionTime = resolutionTime;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
