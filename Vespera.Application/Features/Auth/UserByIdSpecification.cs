using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Auth;

public sealed class UserByIdSpecification : ISpecification<User>
{
    public UserByIdSpecification(UserId userId)
    {
        Criteria = user => user.Id == userId;
    }

    public Expression<Func<User, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<User, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<User, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
