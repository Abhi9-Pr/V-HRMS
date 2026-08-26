using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PayslipByIdSpecification : ISpecification<Payslip>
{
    public PayslipByIdSpecification(PayslipId id)
    {
        Criteria = payslip => payslip.Id == id;
    }

    public Expression<Func<Payslip, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Payslip, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Payslip, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
