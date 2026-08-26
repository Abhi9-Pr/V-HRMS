using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record PayrollLineDto(Guid EmployeeId, decimal Gross, decimal Deductions, decimal Net, decimal LossOfPayDays);

public sealed record PayrollRunDto(
    Guid Id, int Month, int Year, string Status, string? DryRunExecutedBy, string? FreezeOverriddenBy,
    string? FreezeOverrideReason, IReadOnlyList<PayrollLineDto> Lines);

public sealed record GetPayrollRunQuery(Guid PayrollRunId) : IRequest<Result<PayrollRunDto>>;
