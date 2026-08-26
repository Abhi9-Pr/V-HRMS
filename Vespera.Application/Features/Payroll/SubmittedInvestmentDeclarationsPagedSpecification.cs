using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class SubmittedInvestmentDeclarationsPagedSpecification : ISpecification<InvestmentDeclaration>
{
    public SubmittedInvestmentDeclarationsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = declaration => declaration.TenantId == tenantId && declaration.Status == InvestmentDeclarationStatus.Submitted;
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<InvestmentDeclaration, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclaration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclaration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging { get; }
}
