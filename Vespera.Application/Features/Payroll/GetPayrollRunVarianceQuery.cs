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

/// <summary><see cref="Flags"/> is <see cref="PayrollVarianceFlag"/> serialized to its string
/// name, not the enum itself — a JSON integer enum with no name metadata is useless to the
/// generated TypeScript client (NSwag emits placeholder member names like <c>_0</c>/<c>_1</c> with
/// nothing to map them back to), the same reason <c>PayrollRunDto.Status</c> is a string.</summary>
public sealed record PayrollVarianceLine(
    Guid EmployeeId, decimal? PriorNet, decimal? CurrentNet, decimal? VariancePercent, IReadOnlyList<string> Flags);

public sealed record GetPayrollRunVarianceQuery(Guid PayrollRunId) : IRequest<Result<IReadOnlyList<PayrollVarianceLine>>>;
