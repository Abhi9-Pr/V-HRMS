using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class ExitEmployeeCommandHandler : IRequestHandler<ExitEmployeeCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExitEmployeeCommandHandler(IReadRepository<Employee> employees, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _employees = employees;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ExitEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(new EmployeeId(request.EmployeeId)), cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        return employee.Exit(request.ExitDate, request.Reason, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
