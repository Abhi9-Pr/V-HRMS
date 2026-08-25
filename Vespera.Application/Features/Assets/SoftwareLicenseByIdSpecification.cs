using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class SoftwareLicenseByIdSpecification : ISpecification<SoftwareLicense>
{
    public SoftwareLicenseByIdSpecification(SoftwareLicenseId licenseId)
    {
        Criteria = license => license.Id == licenseId;
    }

    public Expression<Func<SoftwareLicense, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SoftwareLicense, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SoftwareLicense, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
