using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Expenses;

/// <summary>Resolves the signed-in <see cref="ICurrentUser"/> to the <see cref="EmployeeId"/> its
/// account is linked to (<see cref="User.EmployeeId"/>). Null when there is no signed-in user or
/// the account isn't linked to an employee record.</summary>
public sealed class CurrentEmployeeResolver
{
    private readonly IReadRepository<User> _users;
    private readonly ICurrentUser _currentUser;

    public CurrentEmployeeResolver(IReadRepository<User> users, ICurrentUser currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<EmployeeId?> ResolveAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return null;
        }

        var user = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userId)), cancellationToken);
        return user?.EmployeeId;
    }
}
