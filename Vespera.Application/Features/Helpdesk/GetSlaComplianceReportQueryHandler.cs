using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

/// <summary>A genuine cross-aggregate read (Ticket joined to TicketCategory for a display name),
/// so it goes through <see cref="IVesperaDbContext"/> directly per CONTRIBUTING-slices.md's
/// guidance, mirroring <c>GetPublicJobsQueryHandler</c>'s approach. "Breached" is
/// <see cref="Ticket.SlaBreachNotified"/> rather than re-deriving it from ResolvedAt vs. DueAt —
/// they agree by construction (the flag is only ever set when a breach check found the ticket
/// past due), and the flag is what a periodic sweep actually populates.</summary>
public sealed class GetSlaComplianceReportQueryHandler : IRequestHandler<GetSlaComplianceReportQuery, Result<SlaComplianceReportDto>>
{
    private readonly IVesperaDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public GetSlaComplianceReportQueryHandler(IVesperaDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public Task<Result<SlaComplianceReportDto>> Handle(GetSlaComplianceReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var resolvedTickets = (
            from ticket in _dbContext.Set<Ticket>()
            where ticket.TenantId == tenantId && (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
            join category in _dbContext.Set<TicketCategory>() on ticket.CategoryId equals category.Id
            select new { ticket.CategoryId, category.Name, ticket.SlaBreachNotified })
            .ToList();

        var total = resolvedTickets.Count;
        var breached = resolvedTickets.Count(t => t.SlaBreachNotified);
        var onTime = total - breached;
        var compliance = total == 0 ? 100.0 : onTime / (double)total * 100.0;

        var byCategory = resolvedTickets
            .GroupBy(t => (t.CategoryId, t.Name))
            .Select(group =>
            {
                var groupTotal = group.Count();
                var groupBreached = group.Count(t => t.SlaBreachNotified);
                var groupCompliance = groupTotal == 0 ? 100.0 : (groupTotal - groupBreached) / (double)groupTotal * 100.0;
                return new CategoryComplianceRowDto(group.Key.CategoryId.Value, group.Key.Name, groupTotal, groupBreached, groupCompliance);
            })
            .ToList();

        return Task.FromResult(Result.Success(new SlaComplianceReportDto(total, breached, onTime, compliance, byCategory)));
    }
}
