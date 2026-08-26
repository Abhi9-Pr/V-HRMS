using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class InvestmentDeclarationByEmployeeAndFySpecification : ISpecification<InvestmentDeclaration>
{
    public InvestmentDeclarationByEmployeeAndFySpecification(TenantId tenantId, EmployeeId employeeId, string financialYear)
    {
        Criteria = declaration =>
            declaration.TenantId == tenantId && declaration.EmployeeId == employeeId && declaration.FinancialYear == financialYear;
    }

    public Expression<Func<InvestmentDeclaration, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclaration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclaration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
