using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record PublishPayrollRunCommand(Guid PayrollRunId) : IRequest<Result>;
