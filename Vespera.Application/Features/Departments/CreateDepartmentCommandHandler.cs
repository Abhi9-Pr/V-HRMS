using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IWriteRepository<Department> _departments;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateDepartmentCommandHandler(
        IWriteRepository<Department> departments,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _departments = departments;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var parentDepartmentId = request.ParentDepartmentId is { } parentId
            ? new DepartmentId(parentId)
            : (DepartmentId?)null;

        var result = Department.Create(
            _tenantContext.TenantId,
            request.Name,
            request.Code,
            parentDepartmentId,
            _dateTimeProvider.UtcNow,
            _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _departments.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
