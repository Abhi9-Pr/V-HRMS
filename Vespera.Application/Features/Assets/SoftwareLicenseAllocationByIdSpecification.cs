using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class SoftwareLicenseAllocationByIdSpecification : ISpecification<SoftwareLicenseAllocation>
{
    public SoftwareLicenseAllocationByIdSpecification(SoftwareLicenseAllocationId allocationId)
    {
        Criteria = allocation => allocation.Id == allocationId;
    }

    public Expression<Func<SoftwareLicenseAllocation, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SoftwareLicenseAllocation, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SoftwareLicenseAllocation, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
