using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record CreateEmployeeCommand(
    string Code,
    string FirstName,
    string LastName,
    string WorkEmail,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly DateOfJoining,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
