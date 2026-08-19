using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record RegisterUserCommand(string Email, string Password, Guid? EmployeeId, IReadOnlyCollection<Guid> RoleIds)
    : IRequest<Result<Guid>>;
