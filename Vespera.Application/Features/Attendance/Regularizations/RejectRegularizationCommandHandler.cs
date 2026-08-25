using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed class RejectRegularizationCommandHandler : IRequestHandler<RejectRegularizationCommand, Result>
{
    private readonly IReadRepository<RegularizationRequest> _requests;
    private readonly IWriteRepository<RegularizationRequest> _requestWriter;
    private readonly IReadRepository<User> _users;
    private readonly RegularizationApproverResolver _approverResolver;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RejectRegularizationCommandHandler(
        IReadRepository<RegularizationRequest> requests,
        IWriteRepository<RegularizationRequest> requestWriter,
        IReadRepository<User> users,
        RegularizationApproverResolver approverResolver,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _requests = requests;
        _requestWriter = requestWriter;
        _users = users;
        _approverResolver = approverResolver;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RejectRegularizationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;

        var regularizationRequest = await _requests.FirstOrDefaultAsync(
            new RegularizationRequestByIdSpecification(tenantId, new RegularizationRequestId(request.RequestId)), cancellationToken);
        if (regularizationRequest is null)
        {
            return Result.Failure(Error.NotFound("regularization_request.not_found", "Regularization request not found."));
        }

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("regularization_request.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        var asOf = DateOnly.FromDateTime(now.UtcDateTime);
        var authorizedApproverId = await _approverResolver.ResolveAuthorizedApproverAsync(
            regularizationRequest.EmployeeId, asOf, cancellationToken);

        if (callerUser?.EmployeeId is null || authorizedApproverId is null || callerUser.EmployeeId != authorizedApproverId)
        {
            return Result.Failure(Error.Forbidden(
                "regularization_request.not_authorized_approver", "You are not authorized to reject this request."));
        }

        var result = regularizationRequest.Reject(callerUser.EmployeeId.Value, request.RejectionReason, now);
        if (result.IsFailure)
        {
            return result;
        }

        _requestWriter.Update(regularizationRequest);
        return Result.Success();
    }
}
