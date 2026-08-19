using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

/// <summary>Every not-yet-revoked token sharing a rotation family — what reuse detection revokes.</summary>
public sealed class ActiveRefreshTokensByFamilySpecification : ISpecification<RefreshToken>
{
    public ActiveRefreshTokensByFamilySpecification(Guid familyId)
    {
        Criteria = token => token.FamilyId == familyId && token.RevokedAt == null;
    }

    public Expression<Func<RefreshToken, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<RefreshToken, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RefreshToken, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
