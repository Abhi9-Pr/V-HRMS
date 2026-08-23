using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees;

public enum EmployeePiiField
{
    Pan,
    BankAccount,
    AnnualCtc,
}

/// <summary>
/// The only path that ever returns an employee's real PAN, bank account, or compensation value.
/// Gated at the controller by <c>Permissions.EmployeeDocuments.Unmask</c> — distinct from
/// <c>Employees.Read</c>, so viewing a masked profile never implies the right to unmask it — and
/// every successful reveal writes an <c>IPiiAccessAuditor</c> entry.
/// </summary>
public sealed record RevealEmployeePiiFieldQuery(Guid EmployeeId, EmployeePiiField Field) : IRequest<Result<string>>;
