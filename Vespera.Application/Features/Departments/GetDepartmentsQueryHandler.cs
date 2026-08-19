using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<PagedResult<DepartmentDto>>>
{
    private readonly IReadRepository<Department> _departments;
    private readonly ITenantContext _tenantContext;

    public GetDepartmentsQueryHandler(IReadRepository<Department> departments, ITenantContext tenantContext)
    {
        _departments = departments;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PagedResult<DepartmentDto>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var specification = new DepartmentsPagedSpecification(_tenantContext.TenantId, request.Paging);

        var departments = await _departments.ListAsync(specification, cancellationToken);
        var totalCount = await _departments.CountAsync(specification, cancellationToken);

        var items = departments.Adapt<List<DepartmentDto>>();

        return Result.Success(new PagedResult<DepartmentDto>(items, request.Paging.Page, request.Paging.PageSize, totalCount));
    }
}
