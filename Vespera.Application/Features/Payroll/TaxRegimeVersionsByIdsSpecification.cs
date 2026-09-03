using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>Batch counterpart to <see cref="TaxRegimeVersionByIdSpecification"/> — one query for
/// every distinct tax regime version a payroll dry-run needs, instead of one per employee. See
/// <see cref="RunDryRunCommandHandler"/>.</summary>
public sealed class TaxRegimeVersionsByIdsSpecification : ISpecification<TaxRegimeVersion>
{
    public TaxRegimeVersionsByIdsSpecification(IReadOnlyCollection<TaxRegimeVersionId> ids)
    {
        Criteria = version => ids.Contains(version.Id);
    }

    public Expression<Func<TaxRegimeVersion, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<TaxRegimeVersion, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<TaxRegimeVersion, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
