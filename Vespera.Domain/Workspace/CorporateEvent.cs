using Vespera.Domain.Common;

namespace Vespera.Domain.Workspace;

public readonly record struct CorporateEventId(Guid Value)
{
    public static CorporateEventId New() => new(Guid.NewGuid());
}

public sealed class CorporateEvent : AuditableTenantAggregateRoot<CorporateEventId>
{
    private CorporateEvent(
        CorporateEventId id, TenantId tenantId, string title, string description, DateTimeOffset startsAt,
        DateTimeOffset endsAt, string locationText, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Title = title;
        Description = description;
        StartsAt = startsAt;
        EndsAt = endsAt;
        LocationText = locationText;
        IsCancelled = false;
    }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public string LocationText { get; private set; }

    public bool IsCancelled { get; private set; }

    public static Result<CorporateEvent> Create(
        TenantId tenantId, string title, string description, DateTimeOffset startsAt, DateTimeOffset endsAt,
        string locationText, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<CorporateEvent>(Error.Validation("corporate_event.title_required", "Title is required."));
        }

        if (endsAt <= startsAt)
        {
            return Result.Failure<CorporateEvent>(Error.Validation("corporate_event.invalid_window", "End time must be after start time."));
        }

        return Result.Success(new CorporateEvent(
            CorporateEventId.New(), tenantId, title.Trim(), description, startsAt, endsAt, locationText, occurredOn, createdBy));
    }

    public Result Reschedule(DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset occurredOn, string modifiedBy)
    {
        if (endsAt <= startsAt)
        {
            return Result.Failure(Error.Validation("corporate_event.invalid_window", "End time must be after start time."));
        }

        StartsAt = startsAt;
        EndsAt = endsAt;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (IsCancelled)
        {
            return Result.Failure(Error.Conflict("corporate_event.already_cancelled", "Event is already cancelled."));
        }

        IsCancelled = true;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
