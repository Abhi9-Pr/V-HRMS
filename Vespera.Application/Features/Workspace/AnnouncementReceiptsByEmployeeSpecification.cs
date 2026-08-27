using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class AnnouncementReceiptsByEmployeeSpecification : ISpecification<AnnouncementReceipt>
{
    public AnnouncementReceiptsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = receipt => receipt.TenantId == tenantId && receipt.EmployeeId == employeeId;
    }

    public Expression<Func<AnnouncementReceipt, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<AnnouncementReceipt, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AnnouncementReceipt, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
