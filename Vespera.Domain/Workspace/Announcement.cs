using Vespera.Domain.Common;

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

public sealed class Announcement : AuditableTenantAggregateRoot<AnnouncementId>
{
    private Announcement(
        AnnouncementId id, TenantId tenantId, string title, string body, AnnouncementAudienceScope audienceScope,
        DateTimeOffset publishAt, DateTimeOffset? expiresAt, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Title = title;
        Body = body;
        AudienceScope = audienceScope;
        PublishAt = publishAt;
        ExpiresAt = expiresAt;
        IsPublished = false;
    }

    public string Title { get; private set; }

    public string Body { get; private set; }

    public AnnouncementAudienceScope AudienceScope { get; }

    public DateTimeOffset PublishAt { get; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public bool IsPublished { get; private set; }

    public static Result<Announcement> Create(
        TenantId tenantId, string title, string body, AnnouncementAudienceScope audienceScope, DateTimeOffset publishAt,
        DateTimeOffset? expiresAt, DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.title_required", "Title is required."));
        }

        if (expiresAt is { } expiry && expiry <= publishAt)
        {
            return Result.Failure<Announcement>(Error.Validation("announcement.invalid_expiry", "Expiry must be after the publish date."));
        }

        return Result.Success(new Announcement(
            AnnouncementId.New(), tenantId, title.Trim(), body, audienceScope, publishAt, expiresAt, occurredOn, createdBy));
    }

    public Result Publish(DateTimeOffset occurredOn, string modifiedBy)
    {
        if (IsPublished)
        {
            return Result.Failure(Error.Conflict("announcement.already_published", "Announcement is already published."));
        }

        IsPublished = true;
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
