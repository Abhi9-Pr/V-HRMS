using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record TransferEmployeeCommand(
    Guid Id,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    DateOnly EffectiveDate,
    string Reason) : IRequest<Result>;
