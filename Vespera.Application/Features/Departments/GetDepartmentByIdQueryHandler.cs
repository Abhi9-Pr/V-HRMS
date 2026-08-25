using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, Result<DepartmentDto>>
{
    private readonly IReadRepository<Department> _departments;
    private readonly ITenantContext _tenantContext;

    public GetDepartmentByIdQueryHandler(IReadRepository<Department> departments, ITenantContext tenantContext)
    {
        _departments = departments;
        _tenantContext = tenantContext;
    }

    public async Task<Result<DepartmentDto>> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new DepartmentByIdSpecification(_tenantContext.TenantId, new DepartmentId(request.Id));

        var department = await _departments.FirstOrDefaultAsync(specification, cancellationToken);
        if (department is null)
        {
            return Result.Failure<DepartmentDto>(Error.NotFound("department.not_found", "Department not found."));
        }

        return Result.Success(department.Adapt<DepartmentDto>());
    }
}
