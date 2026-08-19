using Vespera.Domain.Common;

namespace Vespera.Domain.Compliance;

public readonly record struct RetentionPolicyId(Guid Value)
{
    public static RetentionPolicyId New() => new(Guid.NewGuid());
}

public enum RetentionAction
{
    Purge,
    Anonymize,
}

/// <summary>
/// One DPDP retention rule: how long a category of personal data may be kept, and what happens
/// to it once that window closes. <see cref="EntityCategory"/> is a free-form discriminator (e.g.
/// "Employee.Document", "Attendance.Punch") rather than a CLR type reference, since retention
/// rules are configuration data, not something the entities they govern need to know about.
/// </summary>
public sealed class RetentionPolicy : AuditableTenantAggregateRoot<RetentionPolicyId>
{
    private RetentionPolicy(
        RetentionPolicyId id, TenantId tenantId, string entityCategory, int retentionPeriodDays, RetentionAction action,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        EntityCategory = entityCategory;
        RetentionPeriodDays = retentionPeriodDays;
        Action = action;
    }

    public string EntityCategory { get; private set; }

    public int RetentionPeriodDays { get; private set; }

    public RetentionAction Action { get; private set; }

    public static Result<RetentionPolicy> Create(
        TenantId tenantId, string entityCategory, int retentionPeriodDays, RetentionAction action,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(entityCategory))
        {
            return Result.Failure<RetentionPolicy>(
                Error.Validation("retention_policy.entity_category_required", "Entity category is required."));
        }

        if (retentionPeriodDays <= 0)
        {
            return Result.Failure<RetentionPolicy>(
                Error.Validation("retention_policy.invalid_period", "Retention period must be a positive number of days."));
        }

        return Result.Success(new RetentionPolicy(
            RetentionPolicyId.New(), tenantId, entityCategory.Trim(), retentionPeriodDays, action, occurredOn, createdBy));
    }

    public Result Reconfigure(int retentionPeriodDays, RetentionAction action, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (retentionPeriodDays <= 0)
        {
            return Result.Failure(Error.Validation("retention_policy.invalid_period", "Retention period must be a positive number of days."));
        }

        RetentionPeriodDays = retentionPeriodDays;
        Action = action;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
