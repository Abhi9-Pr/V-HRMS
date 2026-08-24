using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk.Routing;

/// <summary>Ordered strategy for suggesting who a newly-raised ticket should be assigned to —
/// mirrors <c>Expenses.Policy.IExpensePolicyRule</c>'s shape. A null result means "defer to the
/// next rule" (or leave unassigned if none match).</summary>
public interface ITicketRoutingRule
{
    public int Order { get; }

    public EmployeeId? Evaluate(TicketPriority priority, Department department);
}
