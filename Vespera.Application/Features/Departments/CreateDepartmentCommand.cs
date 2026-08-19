using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

public sealed record CreateDepartmentCommand(
    string Name,
    string Code,
    Guid? ParentDepartmentId,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
