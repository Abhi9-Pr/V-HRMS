using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public sealed record GetEmployeeByIdQuery(Guid Id, DateOnly? AsOf) : IRequest<Result<EmployeeDto>>;

/// <summary>
/// <see cref="MaskedPan"/>/<see cref="MaskedBankAccount"/> are always the value objects' own
/// partial-mask representation — never the real value. Compensation has no equivalent partial
/// mask, so <see cref="HasCompensationOnRecord"/> only signals whether one is set. The real value
/// of any of the three is only ever returned by <see cref="RevealEmployeePiiFieldQuery"/>, which
/// is permission-gated and writes an <c>IPiiAccessAuditor</c> entry — this query never does.
/// </summary>
public sealed record EmployeeDto(
    Guid Id,
    string Code,
    string FirstName,
    string LastName,
    string WorkEmail,
    string Phone,
    DateOnly DateOfBirth,
    DateOnly DateOfJoining,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    string Status,
    DateOnly? ExitDate,
    string? ExitReason,
    string? MaskedPan,
    string? MaskedBankAccount,
    bool HasCompensationOnRecord);

/// <summary>Compact profile for the employee list screen and mobile responses.</summary>
public sealed record EmployeeSummaryDto(Guid Id, string Code, string FullName, string Status);
