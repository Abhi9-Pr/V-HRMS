using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Infrastructure.Identity;

/// <summary>
/// Deactivates the <see cref="User"/> row and revokes every active <see cref="RefreshToken"/> for
/// it — the same token-revocation logic <c>LogoutAllDevicesCommandHandler</c> uses, reused here
/// rather than duplicated. Reads via <see cref="IReadRepositoryAdmin{T}"/> because this typically
/// runs from a background sweep with no ambient tenant context (see
/// <c>OffboardingAccessRevocationHostedService</c>), not because it needs to cross tenants for
/// its own sake.
/// </summary>
public sealed class AccessRevocationService : IAccessRevocationService
{
    private readonly IReadRepositoryAdmin<User> _usersAdmin;
    private readonly IWriteRepository<User> _userWriter;
    private readonly IReadRepositoryAdmin<RefreshToken> _refreshTokensAdmin;
    private readonly IWriteRepository<RefreshToken> _refreshTokenWriter;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AccessRevocationService(
        IReadRepositoryAdmin<User> usersAdmin,
        IWriteRepository<User> userWriter,
        IReadRepositoryAdmin<RefreshToken> refreshTokensAdmin,
        IWriteRepository<RefreshToken> refreshTokenWriter,
        IDateTimeProvider dateTimeProvider)
    {
        _usersAdmin = usersAdmin;
        _userWriter = userWriter;
        _refreshTokensAdmin = refreshTokensAdmin;
        _refreshTokenWriter = refreshTokenWriter;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task RevokeAccessAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var typedUserId = new UserId(userId);

        var users = await _usersAdmin.ListIgnoringFiltersAsync(new UserByIdSpecification(typedUserId), cancellationToken);
        var user = users.Count > 0 ? users[0] : null;
        if (user is not null)
        {
            var deactivateResult = user.Deactivate(now, "system");
            if (deactivateResult.IsSuccess)
            {
                _userWriter.Update(user);
            }
        }

        var tokens = await _refreshTokensAdmin.ListIgnoringFiltersAsync(
            new ActiveRefreshTokensByUserSpecification(typedUserId), cancellationToken);
        foreach (var token in tokens)
        {
            token.Revoke(now);
            _refreshTokenWriter.Update(token);
        }
    }
}
