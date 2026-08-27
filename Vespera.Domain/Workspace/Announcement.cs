using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace.Events;

namespace Vespera.Domain.Workspace;

public readonly record struct AnnouncementId(Guid Value)
{
    public static AnnouncementId New() => new(Guid.NewGuid());
}

public enum AnnouncementAudienceScope
{
    AllEmployees,
    Department,
    Location,
}

/// <summary>Ordering weight only — does not gate delivery. <see cref="Critical"/> and
/// <see cref="High"/> sort above <see cref="Normal"/>/<see cref="Low"/> in the dashboard widget,
/// same-priority ties broken by <see cref="Announcement.PublishAt"/> (newest first).</summary>
public enum AnnouncementPriority
{
    Low,
    Normal,
    High,
    Critical,
}

public sealed class Announcement : AuditableTenantAggregateRoot<AnnouncementId>
{
    private Announcement(
        AnnouncementId id, TenantId tenantId, string title, string body, AnnouncementAudienceScope audienceScope,
        DepartmentId? targetDepartmentId, LocationId? targetLocationId, AnnouncementPriority priority, DateTimeOffset publishAt,
        DateTimeOffset? expiresAt, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Title = title;
        Body = body;
        AudienceScope = audienceScope;
        TargetDepartmentId = targetDepartmentId;
        TargetLocationId = targetLocationId;
        Priority = priority;
        PublishAt = publishAt;
        ExpiresAt = expiresAt;
        IsPublished = false;
        IsPinned = false;
    }

    public string Title { get; private set; }

    public string Body { get; private set; }

    public AnnouncementAudienceScope AudienceScope { get; }

    /// <summary>Set only when <see cref="AudienceScope"/> is <see cref="AnnouncementAudienceScope.Department"/>.</summary>
    public DepartmentId? TargetDepartmentId { get; }

    /// <summary>Set only when <see cref="AudienceScope"/> is <see cref="AnnouncementAudienceScope.Location"/>.</summary>
    public LocationId? TargetLocationId { get; }

    public AnnouncementPriority Priority { get; private set; }

    public DateTimeOffset PublishAt { get; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsPublished { get; private set; }

    public bool IsPinned { get; private set; }

    public static Result<Announcement> Create(
        TenantId tenantId, string title, string body, AnnouncementAudienceScope audienceScope, DepartmentId? targetDepartmentId,
        LocationId? targetLocationId, AnnouncementPriority priority, DateTimeOffset publishAt, DateTimeOffset? expiresAt,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.title_required", "Title is required."));
        }

        if (expiresAt is { } expiry && expiry <= publishAt)
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.invalid_expiry", "Expiry must be after the publish date."));
        }

        if (audienceScope == AnnouncementAudienceScope.Department && targetDepartmentId is null)
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.department_required", "A target department is required for a department-scoped announcement."));
        }

        if (audienceScope == AnnouncementAudienceScope.Location && targetLocationId is null)
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.location_required", "A target location is required for a location-scoped announcement."));
        }

        return Result.Success(new Announcement(
            AnnouncementId.New(), tenantId, title.Trim(), body, audienceScope,
            audienceScope == AnnouncementAudienceScope.Department ? targetDepartmentId : null,
            audienceScope == AnnouncementAudienceScope.Location ? targetLocationId : null,
            priority, publishAt, expiresAt, occurredOn, createdBy));
    }

    public Result Publish(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (IsPublished)
        {
            return Result.Failure(Error.Conflict("announcement.already_published", "Announcement is already published."));
        }

        IsPublished = true;
        Touch(occurredOn, modifiedBy);
        Raise(new AnnouncementPublished(Id, TenantId, occurredOn));
        return Result.Success();
    }

    public Result Pin(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (IsPinned)
        {
            return Result.Failure(Error.Conflict("announcement.already_pinned", "Announcement is already pinned."));
        }

        IsPinned = true;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Unpin(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (!IsPinned)
        {
            return Result.Failure(Error.Conflict("announcement.not_pinned", "Announcement is not pinned."));
        }

        IsPinned = false;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }

    public Result Expire(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (!IsPublished)
        {
            return Result.Failure(Error.Conflict("announcement.not_published", "Cannot expire an unpublished announcement."));
        }

        ExpiresAt = occurredOn;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
