using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>Orders by Subject, not a DateTimeOffset column — this codebase's SQLite fallback
/// provider can't translate ORDER BY on DateTimeOffset (see AssetAssignmentsByAssetSpecification's
/// equivalent note from Phase 11b/11c).</summary>
public sealed class TicketsPagedSpecification : ISpecification<Ticket>
{
    public TicketsPagedSpecification(TenantId tenantId, PagedRequest paging)
    {
        Criteria = ticket => ticket.TenantId == tenantId;
        OrderBy = [(ticket => (object)ticket.Subject, paging.SortDescending)];
        Paging = ((paging.Page - 1) * paging.PageSize, paging.PageSize);
    }

    public Expression<Func<Ticket, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<Ticket, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<Ticket, object>> KeySelector, bool Descending)> OrderBy { get; }

    public (int Skip, int Take)? Paging { get; }
}
