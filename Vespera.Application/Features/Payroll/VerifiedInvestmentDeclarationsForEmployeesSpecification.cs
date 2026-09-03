using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>Batch counterpart to <see cref="VerifiedInvestmentDeclarationSpecification"/> — one
/// query for every employee a payroll dry-run needs, instead of one per employee. See
/// <see cref="RunDryRunCommandHandler"/>.</summary>
public sealed class VerifiedInvestmentDeclarationsForEmployeesSpecification : ISpecification<InvestmentDeclaration>
{
    public VerifiedInvestmentDeclarationsForEmployeesSpecification(
        TenantId tenantId, IReadOnlyCollection<EmployeeId> employeeIds, string financialYear)
    {
        Criteria = declaration =>
            declaration.TenantId == tenantId && employeeIds.Contains(declaration.EmployeeId) &&
            declaration.FinancialYear == financialYear && declaration.Status == InvestmentDeclarationStatus.Verified;
    }

    public Expression<Func<InvestmentDeclaration, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<InvestmentDeclaration, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<InvestmentDeclaration, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
