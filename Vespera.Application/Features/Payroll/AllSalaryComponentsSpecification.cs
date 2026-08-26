using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class AllSalaryComponentsSpecification : ISpecification<SalaryComponent>
{
    public AllSalaryComponentsSpecification(TenantId tenantId)
    {
        Criteria = component => component.TenantId == tenantId;
    }

    public Expression<Func<SalaryComponent, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<SalaryComponent, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<SalaryComponent, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
