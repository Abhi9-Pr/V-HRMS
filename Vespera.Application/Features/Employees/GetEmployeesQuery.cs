using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record GetEmployeesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<EmployeeSummaryDto>>>;
