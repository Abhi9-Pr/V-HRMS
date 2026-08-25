using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly IReadRepository<Department> _departments;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteDepartmentCommandHandler(
        IReadRepository<Department> departments,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _departments = departments;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var specification = new DepartmentByIdSpecification(_tenantContext.TenantId, new DepartmentId(request.Id));

        var department = await _departments.FirstOrDefaultAsync(specification, cancellationToken);
        if (department is null)
        {
            return Result.Failure(Error.NotFound("department.not_found", "Department not found."));
        }

        return department.Delete(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
