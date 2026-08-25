using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record ExitEmployeeCommand(Guid Id, DateOnly ExitDate, string Reason) : IRequest<Result>;
