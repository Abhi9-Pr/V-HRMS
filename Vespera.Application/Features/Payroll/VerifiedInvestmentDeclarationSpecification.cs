using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>The one investment declaration whose approved lines feed <c>IncomeTaxRule</c> — only a
/// <see cref="InvestmentDeclarationStatus.Verified"/> declaration counts; a still-<c>Submitted</c>
/// one (finance hasn't finished reviewing every line yet) does not.</summary>
public sealed class VerifiedInvestmentDeclarationSpecification : ISpecification<InvestmentDeclaration>
{
    public VerifiedInvestmentDeclarationSpecification(TenantId tenantId, EmployeeId employeeId, string financialYear)
    {
        Criteria = declaration =>
            declaration.TenantId == tenantId && declaration.EmployeeId == employeeId &&
            declaration.FinancialYear == financialYear && declaration.Status == InvestmentDeclarationStatus.Verified;
    }

    public Expression<Func<InvestmentDeclaration, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclaration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclaration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
