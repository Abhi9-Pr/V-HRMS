using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PayslipsByEmployeeSpecification : ISpecification<Payslip>
{
    public PayslipsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = payslip => payslip.TenantId == tenantId && payslip.EmployeeId == employeeId && payslip.IsPublished;
        OrderBy = [(payslip => (object)payslip.GeneratedAt, true)];
    }

    public Expression<Func<Payslip, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<Payslip, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Payslip, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging => null;
}
