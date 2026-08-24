using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record OpenPayrollRunCommand(int Month, int Year) : IRequest<Result<Guid>>;
