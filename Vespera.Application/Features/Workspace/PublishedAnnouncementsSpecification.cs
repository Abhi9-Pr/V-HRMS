using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

/// <summary>Every currently-live announcement for a tenant, before audience-scope filtering
/// (<see cref="Announcement.AudienceScope"/> can't be expressed as a simple column predicate
/// against "the caller's department/location", so that narrowing happens in memory in
/// <c>GetAnnouncementsForMeQueryHandler</c> — acceptable for the low volume of active
/// announcements a tenant realistically has live at once).</summary>
public sealed class PublishedAnnouncementsSpecification : ISpecification<Announcement>
{
    public PublishedAnnouncementsSpecification(TenantId tenantId, DateTimeOffset asOf)
    {
        Criteria = announcement =>
            announcement.TenantId == tenantId && !announcement.IsDeleted && announcement.IsPublished
            && announcement.PublishAt <= asOf && (announcement.ExpiresAt == null || announcement.ExpiresAt > asOf);
    }

    public Expression<Func<Announcement, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Announcement, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Announcement, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
