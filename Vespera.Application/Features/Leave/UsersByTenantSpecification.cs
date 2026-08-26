using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Leave;

/// <summary>Every active user for a tenant — role membership (<c>RoleIds.Contains(...)</c>) is
/// filtered client-side after materializing, since it's an owned collection and this only ever
/// runs to resolve a single HR approver, not as a hot path.</summary>
public sealed class UsersByTenantSpecification : ISpecification<User>
{
    public UsersByTenantSpecification(TenantId tenantId)
    {
        Criteria = u => u.TenantId == tenantId && u.Status == UserStatus.Active && !u.IsDeleted;
    }

    public Expression<Func<User, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<User, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<User, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
