using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetMyDelegationsQueryHandler : IRequestHandler<GetMyDelegationsQuery, Result<IReadOnlyList<ProxyDelegationDto>>>
{
    private readonly IReadRepository<User> _users;
    private readonly IReadRepository<ProxyDelegation> _delegations;
    private readonly ICurrentUser _currentUser;

    public GetMyDelegationsQueryHandler(IReadRepository<User> users, IReadRepository<ProxyDelegation> delegations, ICurrentUser currentUser)
    {
        _users = users;
        _delegations = delegations;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ProxyDelegationDto>>> Handle(GetMyDelegationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<IReadOnlyList<ProxyDelegationDto>>(Error.Unauthorized("proxy_delegation.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } delegatorId)
        {
            return Result.Success<IReadOnlyList<ProxyDelegationDto>>([]);
        }

        var delegations = await _delegations.ListAsync(new ProxyDelegationsByDelegatorSpecification(delegatorId), cancellationToken);
        var dtos = delegations.Select(d => new ProxyDelegationDto(
            d.Id.Value, d.DelegateId.Value, d.Validity.Start, d.Validity.End, d.Scope.ToString(), d.IsRevoked)).ToList();

        return Result.Success<IReadOnlyList<ProxyDelegationDto>>(dtos);
    }
}
