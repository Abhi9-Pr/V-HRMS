using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>The ledger→projection read — <see cref="LeaveBalanceDto"/> is computed entirely from
/// <see cref="Domain.Leave.LeaveBalance"/>'s own computed properties, never a stored number.</summary>
public sealed record GetLeaveBalanceQuery(Guid LeaveTypeId) : IRequest<Result<LeaveBalanceDto>>;

public sealed record LeaveBalanceDto(
    decimal Available, decimal Accrued, decimal Used, decimal CarriedForward, IReadOnlyList<LeaveLedgerEntryDto> RecentEntries);

public sealed record LeaveLedgerEntryDto(
    Guid Id, string Type, string Direction, decimal Amount, string Reason, DateTimeOffset OccurredOn, string PostedBy);
