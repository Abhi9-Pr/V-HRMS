using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class InvestmentDeclarationWindowByTenantAndFySpecification : ISpecification<InvestmentDeclarationWindow>
{
    public InvestmentDeclarationWindowByTenantAndFySpecification(TenantId tenantId, string financialYear)
    {
        Criteria = window => window.TenantId == tenantId && window.FinancialYear == financialYear;
    }

    public Expression<Func<InvestmentDeclarationWindow, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclarationWindow, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclarationWindow, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
