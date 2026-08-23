using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>The caller's "team inbox" — every leave request whose approval chain's current step
/// resolves (delegate-aware) to the caller, mirroring <c>GetRegularizationsQuery</c>'s shape.</summary>
public sealed record GetApprovalInboxQuery : IRequest<Result<IReadOnlyList<ApprovalInboxItemDto>>>;

public sealed record ApprovalInboxItemDto(
    Guid LeaveRequestId,
    Guid EmployeeId,
    Guid LeaveTypeId,
    DateOnly From,
    DateOnly To,
    decimal RequestedDays,
    string Reason,
    int StepIndex,
    int StepCount,
    bool ActingAsDelegate);
