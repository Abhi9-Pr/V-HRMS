using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Employees;

public sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEmployeeCommandHandler(
        IReadRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var specification = new EmployeeByIdSpecification(_tenantContext.TenantId, new EmployeeId(request.Id));
        var employee = await _employees.FirstOrDefaultAsync(specification, cancellationToken);
        if (employee is null)
        {
            return Result.Failure(Error.NotFound("employee.not_found", "Employee not found."));
        }

        var email = EmailAddress.Create(request.WorkEmail);
        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        var phone = PhoneNumber.Create(request.Phone);
        if (phone.IsFailure)
        {
            return Result.Failure(phone.Error);
        }

        PanNumber? pan = null;
        if (!string.IsNullOrWhiteSpace(request.Pan))
        {
            var panResult = PanNumber.Create(request.Pan);
            if (panResult.IsFailure)
            {
                return Result.Failure(panResult.Error);
            }

            pan = panResult.Value;
        }

        BankAccountNumber? bankAccount = null;
        if (!string.IsNullOrWhiteSpace(request.BankAccount))
        {
            var bankAccountResult = BankAccountNumber.Create(request.BankAccount);
            if (bankAccountResult.IsFailure)
            {
                return Result.Failure(bankAccountResult.Error);
            }

            bankAccount = bankAccountResult.Value;
        }

        Money? annualCtc = null;
        if (request.AnnualCtcAmount is { } amount && request.AnnualCtcCurrency is { } currencyCode)
        {
            if (!Enum.TryParse<Currency>(currencyCode, ignoreCase: true, out var currency))
            {
                return Result.Failure(Error.Validation("employee.invalid_currency", "Unrecognized currency code."));
            }

            annualCtc = Money.Of(amount, currency);
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        var personalDetailsResult = employee.UpdatePersonalDetails(request.FirstName, request.LastName, email.Value, phone.Value, now, modifiedBy);
        if (personalDetailsResult.IsFailure)
        {
            return personalDetailsResult;
        }

        var statutoryResult = employee.UpdateStatutoryDetails(pan, bankAccount, now, modifiedBy);
        if (statutoryResult.IsFailure)
        {
            return statutoryResult;
        }

        return employee.UpdateCompensation(annualCtc, now, modifiedBy);
    }
}
