using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk.Routing;

/// <summary>Every ticket category belongs to a department; route to that department's head unless
/// none is configured (in which case the ticket stays unassigned for manual triage). The simplest
/// correct default — priority-aware routing (e.g. escalate Critical straight to someone else) is a
/// natural follow-on rule, added the OCP way (a new <see cref="ITicketRoutingRule"/> + one DI
/// registration line), not by editing this one.</summary>
public sealed class DefaultToDepartmentHeadRoutingRule : ITicketRoutingRule
{
    public int Order => 1;

    public EmployeeId? Evaluate(TicketPriority priority, Department department) => department.HeadEmployeeId;
}
