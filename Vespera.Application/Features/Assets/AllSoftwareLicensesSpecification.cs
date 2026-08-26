using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class AllSoftwareLicensesSpecification : ISpecification<SoftwareLicense>
{
    public AllSoftwareLicensesSpecification(TenantId tenantId)
    {
        Criteria = license => license.TenantId == tenantId && !license.IsDeleted;
    }

    public Expression<Func<SoftwareLicense, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<SoftwareLicense, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SoftwareLicense, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
