using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class AnnouncementReceiptsByAnnouncementSpecification : ISpecification<AnnouncementReceipt>
{
    public AnnouncementReceiptsByAnnouncementSpecification(TenantId tenantId, AnnouncementId announcementId)
    {
        Criteria = receipt => receipt.TenantId == tenantId && receipt.AnnouncementId == announcementId;
    }

    public Expression<Func<AnnouncementReceipt, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AnnouncementReceipt, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AnnouncementReceipt, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
