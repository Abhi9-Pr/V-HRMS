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
        TimeOnly businessHoursStart, TimeOnly businessHoursEnd, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Name = name;
        ResponseTime = responseTime;
        ResolutionTime = resolutionTime;
        BusinessHoursStart = businessHoursStart;
        BusinessHoursEnd = businessHoursEnd;
    }

    public string Name { get; private set; }

    public TimeSpan ResponseTime { get; private set; }

    public TimeSpan ResolutionTime { get; private set; }

    /// <summary>The business-hours window <see cref="Services.BusinessHoursCalculator"/> uses to
    /// turn <see cref="ResolutionTime"/> into an actual due instant — see
    /// <c>RaiseTicketCommandHandler</c>.</summary>
    public TimeOnly BusinessHoursStart { get; private set; }

    public TimeOnly BusinessHoursEnd { get; private set; }

    public static Result<SlaPolicy> Create(
        TenantId tenantId, string name, TimeSpan responseTime, TimeSpan resolutionTime, DateTimeOffset occurredOn, string createdBy,
        TimeOnly? businessHoursStart = null, TimeOnly? businessHoursEnd = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<SlaPolicy>(Error.Validation("sla_policy.name_required", "SLA policy name is required."));
        }

        if (resolutionTime < responseTime)
        {
            return Result.Failure<SlaPolicy>(Error.Validation("sla_policy.invalid_targets", "Resolution time cannot be shorter than response time."));
        }

        return Result.Success(new SlaPolicy(
            SlaPolicyId.New(), tenantId, name.Trim(), responseTime, resolutionTime,
            businessHoursStart ?? new TimeOnly(9, 0), businessHoursEnd ?? new TimeOnly(18, 0), occurredOn, createdBy));
    }

    public Result UpdateTargets(
        TimeSpan responseTime, TimeSpan resolutionTime, DateTimeOffset occurredOn, string modifiedBy,
        TimeOnly? businessHoursStart = null, TimeOnly? businessHoursEnd = null)
    {
        if (resolutionTime < responseTime)
        {
            return Result.Failure(Error.Validation("sla_policy.invalid_targets", "Resolution time cannot be shorter than response time."));
        }

        ResponseTime = responseTime;
        ResolutionTime = resolutionTime;
        BusinessHoursStart = businessHoursStart ?? BusinessHoursStart;
        BusinessHoursEnd = businessHoursEnd ?? BusinessHoursEnd;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
