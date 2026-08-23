using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public enum PayrollVarianceFlag
{
    NewJoiner,
    Exit,
    LargeVariance,
    ZeroNet,
}

public sealed record PayrollVarianceLine(
    Guid EmployeeId, decimal? PriorNet, decimal? CurrentNet, decimal? VariancePercent, IReadOnlyList<PayrollVarianceFlag> Flags);

public sealed record GetPayrollRunVarianceQuery(Guid PayrollRunId) : IRequest<Result<IReadOnlyList<PayrollVarianceLine>>>;
