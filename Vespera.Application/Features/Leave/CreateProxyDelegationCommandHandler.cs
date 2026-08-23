using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Leave;

public sealed class CreateProxyDelegationCommandHandler : IRequestHandler<CreateProxyDelegationCommand, Result<Guid>>
{
    private readonly IReadRepository<User> _users;
    private readonly IWriteRepository<ProxyDelegation> _delegationWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public CreateProxyDelegationCommandHandler(
        IReadRepository<User> users, IWriteRepository<ProxyDelegation> delegationWriter, ITenantContext tenantContext,
        ICurrentUser currentUser)
    {
        _users = users;
        _delegationWriter = delegationWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateProxyDelegationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized("proxy_delegation.not_authenticated", "Not authenticated."));
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        if (callerUser?.EmployeeId is not { } delegatorId)
        {
            return Result.Failure<Guid>(Error.Validation("proxy_delegation.no_employee_profile", "This user has no linked employee profile."));
        }

        var periodResult = DateRange.Create(request.From, request.To);
        if (periodResult.IsFailure)
        {
            return Result.Failure<Guid>(periodResult.Error);
        }

        var scope = Enum.Parse<DelegationScope>(request.Scope);
        var delegationResult = ProxyDelegation.Create(
            _tenantContext.TenantId, delegatorId, new EmployeeId(request.DelegateEmployeeId), periodResult.Value, scope);
        if (delegationResult.IsFailure)
        {
            return Result.Failure<Guid>(delegationResult.Error);
        }

        await _delegationWriter.AddAsync(delegationResult.Value, cancellationToken);
        return Result.Success(delegationResult.Value.Id.Value);
    }
}
