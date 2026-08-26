using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class SalaryStructuresActiveOnDateSpecification : ISpecification<SalaryStructure>
{
    public SalaryStructuresActiveOnDateSpecification(TenantId tenantId, DateOnly asOf)
    {
        Criteria = structure =>
            structure.TenantId == tenantId && structure.ValidFrom <= asOf && (structure.ValidTo == null || structure.ValidTo >= asOf);
    }

    public Expression<Func<SalaryStructure, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SalaryStructure, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SalaryStructure, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
