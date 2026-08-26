using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class RevokeProxyDelegationCommandHandler : IRequestHandler<RevokeProxyDelegationCommand, Result>
{
    private readonly IReadRepository<ProxyDelegation> _delegations;
    private readonly IWriteRepository<ProxyDelegation> _delegationWriter;
    private readonly IReadRepository<User> _users;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public RevokeProxyDelegationCommandHandler(
        IReadRepository<ProxyDelegation> delegations, IWriteRepository<ProxyDelegation> delegationWriter,
        IReadRepository<User> users, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _delegations = delegations;
        _delegationWriter = delegationWriter;
        _users = users;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RevokeProxyDelegationCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var delegation = await _delegations.FirstOrDefaultAsync(
            new ProxyDelegationByIdSpecification(tenantId, new ProxyDelegationId(request.DelegationId)), cancellationToken);
        if (delegation is null)
        {
            return Result.Failure(Error.NotFound("proxy_delegation.not_found", "Delegation not found."));
        }

        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure(Error.Unauthorized("proxy_delegation.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is null || callerUser.EmployeeId != delegation.DelegatorId)
        {
            return Result.Failure(Error.Forbidden("proxy_delegation.not_the_delegator", "Only the delegator can revoke this delegation."));
        }

        var result = delegation.Revoke();
        if (result.IsFailure)
        {
            return result;
        }

        _delegationWriter.Update(delegation);
        return Result.Success();
    }
}
