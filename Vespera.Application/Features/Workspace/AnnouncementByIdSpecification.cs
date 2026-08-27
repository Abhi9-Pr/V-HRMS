using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class AnnouncementByIdSpecification : ISpecification<Announcement>
{
    public AnnouncementByIdSpecification(TenantId tenantId, AnnouncementId announcementId)
    {
        Criteria = announcement => announcement.TenantId == tenantId && announcement.Id == announcementId && !announcement.IsDeleted;
    }

    public Expression<Func<Announcement, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Announcement, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Announcement, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
