using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard;

public sealed class DashboardLayoutByUserSpecification : ISpecification<DashboardLayout>
{
    public DashboardLayoutByUserSpecification(TenantId tenantId, UserId userId)
    {
        Criteria = layout => layout.TenantId == tenantId && layout.UserId == userId;
    }

    public Expression<Func<DashboardLayout, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<DashboardLayout, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<DashboardLayout, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
