using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string WorkEmail,
    string Phone,
    string? Pan,
    string? BankAccount,
    decimal? AnnualCtcAmount,
    string? AnnualCtcCurrency) : IRequest<Result>;
