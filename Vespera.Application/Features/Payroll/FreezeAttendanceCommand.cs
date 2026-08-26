using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record FreezeAttendanceCommand(Guid PayrollRunId, string? OverrideReason) : IRequest<Result>;
