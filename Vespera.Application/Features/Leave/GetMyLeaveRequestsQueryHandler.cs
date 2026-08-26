using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetMyLeaveRequestsQueryHandler : IRequestHandler<GetMyLeaveRequestsQuery, Result<IReadOnlyList<LeaveRequestDto>>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public GetMyLeaveRequestsQueryHandler(
        IReadRepository<User> users, IReadRepository<LeaveRequest> leaveRequests, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _users = users;
        _leaveRequests = leaveRequests;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<LeaveRequestDto>>> Handle(GetMyLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<IReadOnlyList<LeaveRequestDto>>(Error.Unauthorized("leave_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } employeeId)
        {
            return Result.Success<IReadOnlyList<LeaveRequestDto>>([]);
        }

        var requests = await _leaveRequests.ListAsync(
            new LeaveRequestsByEmployeeSpecification(_tenantContext.TenantId, employeeId), cancellationToken);

        var dtos = requests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new LeaveRequestDto(
                r.Id.Value, r.EmployeeId.Value, r.LeaveTypeId.Value, r.Period.Start, r.Period.End, r.RequestedDays, r.LossOfPayDays,
                r.Reason, r.Status.ToString()))
            .ToList();

        return Result.Success<IReadOnlyList<LeaveRequestDto>>(dtos);
    }
}
