using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class EventRsvpByEventAndEmployeeSpecification : ISpecification<EventRsvp>
{
    public EventRsvpByEventAndEmployeeSpecification(TenantId tenantId, CorporateEventId corporateEventId, EmployeeId employeeId)
    {
        Criteria = rsvp => rsvp.TenantId == tenantId && rsvp.CorporateEventId == corporateEventId && rsvp.EmployeeId == employeeId;
    }

    public Expression<Func<EventRsvp, bool>> Criteria { get; }

    public IReadOnlyList<Expression<Func<EventRsvp, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<EventRsvp, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
