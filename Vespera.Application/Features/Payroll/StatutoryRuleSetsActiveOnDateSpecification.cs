using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class StatutoryRuleSetsActiveOnDateSpecification : ISpecification<StatutoryRuleSet>
{
    public StatutoryRuleSetsActiveOnDateSpecification(TenantId tenantId, DateOnly asOf)
    {
        Criteria = rule => rule.TenantId == tenantId && rule.ValidFrom <= asOf && (rule.ValidTo == null || rule.ValidTo >= asOf);
    }

    public Expression<Func<StatutoryRuleSet, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<StatutoryRuleSet, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<StatutoryRuleSet, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
