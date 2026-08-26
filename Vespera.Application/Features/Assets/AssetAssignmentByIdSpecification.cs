using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class AssetAssignmentByIdSpecification : ISpecification<AssetAssignment>
{
    public AssetAssignmentByIdSpecification(AssetAssignmentId assignmentId)
    {
        Criteria = assignment => assignment.Id == assignmentId;
    }

    public Expression<Func<AssetAssignment, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<AssetAssignment, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<AssetAssignment, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
