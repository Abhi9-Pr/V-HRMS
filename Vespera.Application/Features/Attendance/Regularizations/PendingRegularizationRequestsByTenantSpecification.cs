using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>All pending requests for the tenant, unfiltered by approver — the "who can act on
/// this one" filter happens in memory in <see cref="GetRegularizationsQueryHandler"/> via
/// <see cref="RegularizationApproverResolver"/>, since which manager (or delegate) is authorized
/// for a given request isn't a column this specification's <c>Criteria</c> can express.</summary>
public sealed class PendingRegularizationRequestsByTenantSpecification : ISpecification<RegularizationRequest>
{
    public PendingRegularizationRequestsByTenantSpecification(TenantId tenantId)
    {
        Criteria = r => r.TenantId == tenantId && r.Status == RegularizationStatus.Pending;
    }

    public Expression<Func<RegularizationRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<RegularizationRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RegularizationRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
