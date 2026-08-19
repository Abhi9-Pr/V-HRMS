using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class RefreshTokenByHashSpecification : ISpecification<RefreshToken>
{
    public RefreshTokenByHashSpecification(string tokenHash)
    {
        Criteria = token => token.TokenHash == tokenHash;
    }

    public Expression<Func<RefreshToken, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<RefreshToken, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<RefreshToken, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
