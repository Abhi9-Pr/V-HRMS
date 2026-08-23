using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>The caller's "team inbox" — every pending request the caller is currently the
/// authorized approver for, per <see cref="RegularizationApproverResolver"/> (their own manager
/// role, or an active delegation standing in for someone else's).</summary>
public sealed record GetRegularizationsQuery : IRequest<Result<IReadOnlyList<RegularizationRequestDto>>>;

public sealed record RegularizationRequestDto(
    Guid Id,
    Guid EmployeeId,
    Guid AttendanceDayId,
    string Reason,
    string? EvidenceFileReference,
    string Status,
    Guid? ApproverId,
    string? RejectionReason);
