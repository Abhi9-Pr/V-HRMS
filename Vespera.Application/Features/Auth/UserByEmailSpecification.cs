using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Auth;

/// <summary>Looks up a user by email within the ambient tenant. The tenant global query filter
/// (see VesperaDbContext) already scopes this to the current tenant — Criteria only needs the
/// email match — but login resolves the tenant from the pre-auth X-Tenant-Id header rather than a
/// JWT, so this stays an explicit spec rather than folding into a generic "by email" helper that
/// might get reused somewhere the tenant isn't guaranteed to be right.</summary>
public sealed class UserByEmailSpecification : ISpecification<User>
{
    public UserByEmailSpecification(EmailAddress email)
    {
        // Compares the whole value object, not `.Value` — EF's query translator pushes a
        // value-converted property comparison down to SQL by applying the same converter to both
        // sides; `.Value` is a sub-property access on the materialized CLR type, which EF cannot
        // decompose (that "worked" nowhere — Phase 3 never actually exercised this path). Soft
        // delete isn't repeated here since the tenant global query filter already covers it.
        Criteria = user => user.Email == email;
    }

    public Expression<Func<User, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<User, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<User, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
