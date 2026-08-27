using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class EventRsvpsByEmployeeSpecification : ISpecification<EventRsvp>
{
    public EventRsvpsByEmployeeSpecification(TenantId tenantId, EmployeeId employeeId)
    {
        Criteria = rsvp => rsvp.TenantId == tenantId && rsvp.EmployeeId == employeeId;
    }

    public Expression<Func<EventRsvp, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<EventRsvp, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<EventRsvp, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
