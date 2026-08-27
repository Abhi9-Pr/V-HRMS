using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class AnnouncementReceiptByAnnouncementAndEmployeeSpecification : ISpecification<AnnouncementReceipt>
{
    public AnnouncementReceiptByAnnouncementAndEmployeeSpecification(TenantId tenantId, AnnouncementId announcementId, EmployeeId employeeId)
    {
        Criteria = receipt =>
            receipt.TenantId == tenantId && receipt.AnnouncementId == announcementId && receipt.EmployeeId == employeeId;
    }

    public Expression<Func<AnnouncementReceipt, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AnnouncementReceipt, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AnnouncementReceipt, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
