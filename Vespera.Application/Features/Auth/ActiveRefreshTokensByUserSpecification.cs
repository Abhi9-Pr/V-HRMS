using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

/// <summary>Every not-yet-revoked token for a user, across every device/family — what
/// "logout of all devices" revokes.</summary>
public sealed class ActiveRefreshTokensByUserSpecification : ISpecification<RefreshToken>
{
    public ActiveRefreshTokensByUserSpecification(UserId userId)
    {
        Criteria = token => token.UserId == userId && token.RevokedAt == null;
    }

    public Expression<Func<RefreshToken, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<RefreshToken, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RefreshToken, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
