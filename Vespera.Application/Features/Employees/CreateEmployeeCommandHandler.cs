using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Employees;

public sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IWriteRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateEmployeeCommandHandler(
        IWriteRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var code = EmployeeCode.Create(request.Code);
        if (code.IsFailure)
        {
            return Result.Failure<Guid>(code.Error);
        }

        var email = EmailAddress.Create(request.WorkEmail);
        if (email.IsFailure)
        {
            return Result.Failure<Guid>(email.Error);
        }

        var phone = PhoneNumber.Create(request.Phone);
        if (phone.IsFailure)
        {
            return Result.Failure<Guid>(phone.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var result = Employee.Onboard(
            _tenantContext.TenantId, code.Value, request.FirstName, request.LastName, email.Value, phone.Value,
            request.DateOfBirth, request.DateOfJoining,
            new DepartmentId(request.DepartmentId), new DesignationId(request.DesignationId), new LocationId(request.LocationId),
            now, createdBy);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _employees.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
