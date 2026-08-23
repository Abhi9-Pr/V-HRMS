using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class TaxRegimeVersionByIdSpecification : ISpecification<TaxRegimeVersion>
{
    public TaxRegimeVersionByIdSpecification(TaxRegimeVersionId id)
    {
        Criteria = version => version.Id == id;
    }

    public Expression<Func<TaxRegimeVersion, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<TaxRegimeVersion, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<TaxRegimeVersion, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
