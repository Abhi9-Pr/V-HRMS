using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class SlaPolicyByIdSpecification : ISpecification<SlaPolicy>
{
    public SlaPolicyByIdSpecification(SlaPolicyId policyId)
    {
        Criteria = policy => policy.Id == policyId;
    }

    public Expression<Func<SlaPolicy, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SlaPolicy, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SlaPolicy, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
