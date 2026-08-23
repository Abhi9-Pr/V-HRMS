using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Employees;

public sealed class RevealEmployeePiiFieldQueryHandler : IRequestHandler<RevealEmployeePiiFieldQuery, Result<string>>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public RevealEmployeePiiFieldQueryHandler(
        IReadRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        IPiiAccessAuditor piiAccessAuditor)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<string>> Handle(RevealEmployeePiiFieldQuery request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<string>(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var value = request.Field switch
        {
            EmployeePiiField.Pan => employee.Pan?.Value,
            EmployeePiiField.BankAccount => employee.BankAccount?.Value,
            EmployeePiiField.AnnualCtc => employee.CurrentAnnualCtc?.ToString(),
            _ => null,
        };

        if (value is null)
        {
            return Result.Failure<string>(Error.NotFound("employee.pii_field_not_set", "That field has no value on record."));
        }

        var now = _dateTimeProvider.UtcNow;
        var accessedBy = _currentUser.UserId?.ToString() ?? "system";
        await _piiAccessAuditor.RecordAccessAsync(
            _tenantContext.TenantId, "Employee", employee.Id.Value, request.Field.ToString(), accessedBy, now, cancellationToken);

        return Result.Success(value);
    }
}
