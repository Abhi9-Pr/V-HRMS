using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class TodoItemsByOwnerSpecification : ISpecification<TodoItem>
{
    public TodoItemsByOwnerSpecification(TenantId tenantId, EmployeeId ownerEmployeeId)
    {
        Criteria = item => item.TenantId == tenantId && item.OwnerEmployeeId == ownerEmployeeId;
        OrderBy = [(item => (object)item.SortOrder, false)];
    }

    public Expression<Func<TodoItem, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<TodoItem, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<TodoItem, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging => null;
}
