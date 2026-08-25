using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

public sealed record GetDepartmentByIdQuery(Guid Id) : IRequest<Result<DepartmentDto>>;
