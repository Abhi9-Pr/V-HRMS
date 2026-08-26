using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record GetMyLeaveRequestsQuery : IRequest<Result<IReadOnlyList<LeaveRequestDto>>>;

public sealed record LeaveRequestDto(
    Guid Id,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly From,
    DateOnly To,
    decimal RequestedDays,
    decimal LossOfPayDays,
    string Reason,
    string Status);
