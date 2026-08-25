using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk.Routing;

public sealed class TicketRoutingEvaluator
{
    private readonly IReadOnlyList<ITicketRoutingRule> _rules;

    public TicketRoutingEvaluator(IEnumerable<ITicketRoutingRule> rules)
    {
        _rules = rules.OrderBy(rule => rule.Order).ToList();
    }

    public EmployeeId? Evaluate(TicketPriority priority, Department department)
    {
        foreach (var rule in _rules)
        {
            if (rule.Evaluate(priority, department) is { } employeeId)
            {
                return employeeId;
            }
        }

        return null;
    }
}
