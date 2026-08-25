using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class GetEmployeesQueryHandler : IRequestHandler<GetEmployeesQuery, Result<PagedResult<EmployeeSummaryDto>>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;

    public GetEmployeesQueryHandler(IReadRepository<Employee> employees, ITenantContext tenantContext)
    {
        _employees = employees;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<EmployeeSummaryDto>>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var specification = new EmployeesPagedSpecification(_tenantContext.TenantId, request.Paging);

        var employees = await _employees.ListAsync(specification, cancellationToken);
        var totalCount = await _employees.CountAsync(specification, cancellationToken);

        var items = employees.Adapt<List<EmployeeSummaryDto>>();

        return Result.Success(new PagedResult<EmployeeSummaryDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
