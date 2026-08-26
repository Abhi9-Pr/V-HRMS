using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record GetLeaveTypesQuery : IRequest<Result<IReadOnlyList<LeaveTypeDto>>>;

public sealed record LeaveTypeDto(
    Guid Id, string Name, bool IsPaid, decimal CarryForwardLimit, string? ApplicableGender, int MinimumTenureMonths,
    bool IsEncashable, decimal MaxEncashableDays);
