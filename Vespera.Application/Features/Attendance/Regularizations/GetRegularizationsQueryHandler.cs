using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class GetRegularizationsQueryHandler : IRequestHandler<GetRegularizationsQuery, Result<IReadOnlyList<RegularizationRequestDto>>>
{
    private readonly IReadRepository<Domain.Attendance.RegularizationRequest> _requests;
    private readonly IReadRepository<User> _users;
    private readonly RegularizationApproverResolver _approverResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetRegularizationsQueryHandler(
        IReadRepository<Domain.Attendance.RegularizationRequest> requests,
        IReadRepository<User> users,
        RegularizationApproverResolver approverResolver,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requests = requests;
        _users = users;
        _approverResolver = approverResolver;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<RegularizationRequestDto>>> Handle(
        GetRegularizationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<IReadOnlyList<RegularizationRequestDto>>(
                Error.Unauthorized("regularization_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } callerEmployeeId)
        {
            return Result.Success<IReadOnlyList<RegularizationRequestDto>>([]);
        }

        var asOf = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var pending = await _requests.ListAsync(
            new PendingRegularizationRequestsByTenantSpecification(_tenantContext.TenantId), cancellationToken);

        var actionable = new List<RegularizationRequestDto>();
        foreach (var pendingRequest in pending)
        {
            var authorizedApproverId = await _approverResolver.ResolveAuthorizedApproverAsync(
                pendingRequest.EmployeeId, asOf, cancellationToken);
            if (authorizedApproverId == callerEmployeeId)
            {
                actionable.Add(new RegularizationRequestDto(
                    pendingRequest.Id.Value,
                    pendingRequest.EmployeeId.Value,
                    pendingRequest.AttendanceDayId.Value,
                    pendingRequest.Reason,
                    pendingRequest.EvidenceFileReference,
                    pendingRequest.Status.ToString(),
                    pendingRequest.ApproverId?.Value,
                    pendingRequest.RejectionReason));
            }
        }

        return Result.Success<IReadOnlyList<RegularizationRequestDto>>(actionable);
    }
}
