namespace Vespera.Domain.Common;

/// <summary>
/// Shared plumbing for the common case: a tenant-scoped aggregate that is audited and
/// soft-deletable. Still exposes exactly IAuditable/ISoftDeletable/ITenantScoped — this only
/// removes the ~20 lines of identical property/Touch/Delete/Restore boilerplate that would
/// otherwise be repeated on every such aggregate.
/// </summary>
public abstract class AuditableTenantAggregateRoot<TId> : AggregateRoot<TId>, IAuditable, ISoftDeletable, ITenantScoped
    where TId : notnull
{
    protected AuditableTenantAggregateRoot(TId id, TenantId tenantId, DateTimeOffset createdAt, string createdBy)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);

        TenantId = tenantId;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public TenantId TenantId { get; }

    public DateTimeOffset CreatedAt { get; }

    public string CreatedBy { get; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string? DeletedBy { get; private set; }

    public Result Delete(DateTimeOffset occurredOn, string deletedBy)
    {
        if (IsDeleted)
        {
            return Result.Failure(Error.Conflict("entity.already_deleted", "This record is already deleted."));
        }

        IsDeleted = true;
        DeletedAt = occurredOn;
        DeletedBy = deletedBy;
        return Result.Success();
    }

    public Result Restore()
    {
        if (!IsDeleted)
        {
            return Result.Failure(Error.Conflict("entity.not_deleted", "This record is not deleted."));
        }

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        return Result.Success();
    }

    protected void Touch(DateTimeOffset occurredOn, string modifiedBy)
    {
        ModifiedAt = occurredOn;
        ModifiedBy = modifiedBy;
    }
}
