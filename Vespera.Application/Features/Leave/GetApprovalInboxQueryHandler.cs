using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetApprovalInboxQueryHandler : IRequestHandler<GetApprovalInboxQuery, Result<IReadOnlyList<ApprovalInboxItemDto>>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<ApprovalChain> _chains;
    private readonly IReadRepository<LeaveRequest> _leaveRequests;
    private readonly LeaveApprovalStepAuthorizer _authorizer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetApprovalInboxQueryHandler(
        IReadRepository<User> users, IReadRepository<ApprovalChain> chains, IReadRepository<LeaveRequest> leaveRequests,
        LeaveApprovalStepAuthorizer authorizer, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _chains = chains;
        _leaveRequests = leaveRequests;
        _authorizer = authorizer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<IReadOnlyList<ApprovalInboxItemDto>>> Handle(GetApprovalInboxQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<IReadOnlyList<ApprovalInboxItemDto>>(Error.Unauthorized("leave_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } callerEmployeeId)
        {
            return Result.Success<IReadOnlyList<ApprovalInboxItemDto>>([]);
        }

        var tenantId = _tenantContext.TenantId;
        var asOf = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);

        var chains = await _chains.ListAsync(
            new InProgressApprovalChainsBySubjectTypeSpecification(tenantId, ApprovalSubjectType.LeaveRequest), cancellationToken);

        var items = new List<ApprovalInboxItemDto>();
        foreach (var chain in chains)
        {
            var nominalApproverId = chain.CurrentStep.ApproverId;
            var authorizedApproverId = await _authorizer.ResolveAuthorizedApproverAsync(nominalApproverId, asOf, cancellationToken);
            if (authorizedApproverId != callerEmployeeId)
            {
                continue;
            }

            var leaveRequest = await _leaveRequests.FirstOrDefaultAsync(
                new LeaveRequestByIdSpecification(tenantId, new LeaveRequestId(chain.SubjectId)), cancellationToken);
            if (leaveRequest is null)
            {
                continue;
            }

            items.Add(new ApprovalInboxItemDto(
                leaveRequest.Id.Value, leaveRequest.EmployeeId.Value, leaveRequest.LeaveTypeId.Value, leaveRequest.Period.Start,
                leaveRequest.Period.End, leaveRequest.RequestedDays, leaveRequest.Reason, chain.CurrentStepIndex, chain.Steps.Count,
                authorizedApproverId != nominalApproverId));
        }

        return Result.Success<IReadOnlyList<ApprovalInboxItemDto>>(items);
    }
}
