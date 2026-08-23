using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class RegularizationRequestByIdSpecification : ISpecification<RegularizationRequest>
{
    public RegularizationRequestByIdSpecification(TenantId tenantId, RegularizationRequestId id)
    {
        Criteria = r => r.TenantId == tenantId && r.Id == id;
    }

    public Expression<Func<RegularizationRequest, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<RegularizationRequest, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RegularizationRequest, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
