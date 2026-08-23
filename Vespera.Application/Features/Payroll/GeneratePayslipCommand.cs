using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record GeneratePayslipCommand(Guid PayrollRunId, Guid EmployeeId) : IRequest<Result<Guid>>;
