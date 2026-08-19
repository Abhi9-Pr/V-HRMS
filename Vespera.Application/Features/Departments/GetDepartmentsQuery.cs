using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Departments;

public sealed record GetDepartmentsQuery(PagedRequest Paging) : IRequest<Result<PagedResult<DepartmentDto>>>;

public sealed record DepartmentDto(Guid Id, string Name, string Code, Guid? ParentDepartmentId);

/// <summary>Compact profile for mobile list responses — ID plus display-critical fields only.</summary>
public sealed record DepartmentSummaryDto(Guid Id, string Name);
