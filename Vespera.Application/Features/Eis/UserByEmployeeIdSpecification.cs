using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Eis;

/// <summary>The login (if any) linked to a given employee — used by the offboarding sweep to find
/// whose access to revoke on an employee's exit. Read via <see cref="IReadRepositoryAdmin{T}"/>:
/// this runs from a background job with no ambient tenant context, and the caller already knows
/// the tenant from the <see cref="Employee"/> row it loaded, so it scopes explicitly rather than
/// relying on a query filter that would exclude every row outside an HTTP request.</summary>
public sealed class UserByEmployeeIdSpecification : ISpecification<User>
{
    public UserByEmployeeIdSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = user => user.TenantId == tenantId && user.EmployeeId == employeeId;
    }

    public Expression<Func<User, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<User, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<User, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
