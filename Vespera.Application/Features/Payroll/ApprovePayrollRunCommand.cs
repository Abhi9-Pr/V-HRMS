using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record ApprovePayrollRunCommand(Guid PayrollRunId) : IRequest<Result>;
