using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Helpdesk.Routing;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>Computes the ticket's due instant via <see cref="BusinessHoursCalculator"/> (skipping
/// weekends and the tenant's <see cref="PublicHoliday"/> calendar) rather than plain calendar
/// arithmetic, then runs the routing engine to suggest (and apply) an initial assignee.</summary>
public sealed class RaiseTicketCommandHandler : IRequestHandler<RaiseTicketCommand, Result<Guid>>
{
    private readonly IReadRepository<TicketCategory> _categories;
    private readonly IReadRepository<SlaPolicy> _policies;
    private readonly IReadRepository<PublicHoliday> _holidays;
    private readonly IReadRepository<Department> _departments;
    private readonly IWriteRepository<Ticket> _tickets;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly TicketRoutingEvaluator _routingEvaluator;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public RaiseTicketCommandHandler(
        IReadRepository<TicketCategory> categories,
        IReadRepository<SlaPolicy> policies,
        IReadRepository<PublicHoliday> holidays,
        IReadRepository<Department> departments,
        IWriteRepository<Ticket> tickets,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        TicketRoutingEvaluator routingEvaluator,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _categories = categories;
        _policies = policies;
        _holidays = holidays;
        _departments = departments;
        _tickets = tickets;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _routingEvaluator = routingEvaluator;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<Guid>> Handle(RaiseTicketCommand request, CancellationToken cancellationToken)
    {
        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure<Guid>(Error.Validation("ticket.no_employee", "The signed-in account is not linked to an employee."));
        }

        var categoryId = new TicketCategoryId(request.CategoryId);
        var category = await _categories.FirstOrDefaultAsync(new TicketCategoryByIdSpecification(categoryId), cancellationToken);
        if (category is null)
        {
            return Result.Failure<Guid>(Error.NotFound("ticket_category.not_found", "Ticket category not found."));
        }

        if (category.DefaultSlaPolicyId is not { } slaPolicyId)
        {
            return Result.Failure<Guid>(Error.Validation("ticket.no_sla_policy", "This category has no default SLA policy configured."));
        }

        var policy = await _policies.FirstOrDefaultAsync(new SlaPolicyByIdSpecification(slaPolicyId), cancellationToken);
        if (policy is null)
        {
            return Result.Failure<Guid>(Error.NotFound("sla_policy.not_found", "SLA policy not found."));
        }

        var holidays = await _holidays.ListAsync(new PublicHolidaysByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var holidaySet = holidays.Select(h => h.Date).ToHashSet();

        var now = _dateTimeProvider.UtcNow;
        var dueAt = new BusinessHoursCalculator().AddBusinessHours(
            now, policy.ResolutionTime, policy.BusinessHoursStart, policy.BusinessHoursEnd, holidaySet);

        var ticketResult = Ticket.Raise(
            _tenantContext.TenantId, currentEmployeeId.Value, categoryId, slaPolicyId, request.Subject, request.Description,
            request.Priority, now, dueAt);

        if (ticketResult.IsFailure)
        {
            return Result.Failure<Guid>(ticketResult.Error);
        }

        var ticket = ticketResult.Value;

        var department = await _departments.FirstOrDefaultAsync(new DepartmentByIdSpecification(category.DepartmentId), cancellationToken);
        if (department is not null)
        {
            var suggestedAssignee = _routingEvaluator.Evaluate(request.Priority, department);
            if (suggestedAssignee is { } assigneeId)
            {
                ticket.AssignTo(assigneeId);
            }
        }

        await _tickets.AddAsync(ticket, cancellationToken);
        return Result.Success(ticket.Id.Value);
    }
}
