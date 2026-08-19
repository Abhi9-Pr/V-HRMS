using Vespera.Domain.Common;

namespace Vespera.Domain.IdentityAccess;

public enum TenantStatus
{
    Trial,
    Active,
    Suspended,
}

public sealed class Tenant : AggregateRoot<TenantId>, IAuditable, ISoftDeletable
{
    private Tenant(TenantId id, string name, string code, DateTimeOffset createdAt, string createdBy)
        : base(id)
    {
        Name = name;
        Code = code;
        Status = TenantStatus.Trial;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string Name { get; private set; }

    public string Code { get; }

    public TenantStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public string CreatedBy { get; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedBy { get; private set; }

    public static Result<Tenant> Create(string name, string code, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tenant>(Error.Validation("tenant.name_required", "Tenant name is required."));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Tenant>(Error.Validation("tenant.code_required", "Tenant code is required."));
        }

        return Result.Success(new Tenant(TenantId.New(), name.Trim(), code.Trim(), occurredOn, createdBy));
    }

    public Result Rename(string name, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("tenant.name_required", "Tenant name is required."));
        }

        Name = name.Trim();
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Suspend(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == TenantStatus.Suspended)
        {
            return Result.Failure(Error.Conflict("tenant.already_suspended", "Tenant is already suspended."));
        }

        Status = TenantStatus.Suspended;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Reactivate(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (Status == TenantStatus.Active)
        {
            return Result.Failure(Error.Conflict("tenant.already_active", "Tenant is already active."));
        }

        Status = TenantStatus.Active;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Delete(DateTimeOffset occurredOn, string deletedBy)
    {
        if (IsDeleted)
        {
            return Result.Failure(Error.Conflict("tenant.already_deleted", "Tenant is already deleted."));
        }

        IsDeleted = true;
        DeletedAt = occurredOn;
        DeletedBy = deletedBy;
        return Result.Success();
    }

    private void Touch(DateTimeOffset occurredOn, string modifiedBy)
    {
        ModifiedAt = occurredOn;
        ModifiedBy = modifiedBy;
    }
}
