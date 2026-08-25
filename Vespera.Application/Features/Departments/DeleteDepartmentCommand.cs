using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

public sealed record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;
