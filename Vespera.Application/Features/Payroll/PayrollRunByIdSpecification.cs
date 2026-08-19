using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class PayrollRunByIdSpecification : ISpecification<PayrollRun>
{
    public PayrollRunByIdSpecification(PayrollRunId payrollRunId)
    {
        Criteria = run => run.Id == payrollRunId;
    }

    public Expression<Func<PayrollRun, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<PayrollRun, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<PayrollRun, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
