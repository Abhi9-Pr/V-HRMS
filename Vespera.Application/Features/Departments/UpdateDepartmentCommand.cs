using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

public sealed record UpdateDepartmentCommand(Guid Id, string Name, Guid? ParentDepartmentId) : IRequest<Result>;
