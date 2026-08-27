using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

/// <summary>HR-facing compliance view: who in an announcement's audience has (and hasn't)
/// acknowledged it. Gated by <c>Permissions.Workspace.ManageAnnouncements</c> at the controller —
/// this is the same "genuine cross-aggregate read, materialize and join in memory" shape as
/// <c>GetPendingExpenseApprovalsQueryHandler</c>, acceptable for a per-announcement audience that
/// is at most one tenant's employee roster.</summary>
public sealed class GetAnnouncementReceiptsReportQueryHandler
    : IRequestHandler<GetAnnouncementReceiptsReportQuery, Result<AnnouncementReceiptsReportDto>>
{
    private readonly IReadRepository<Announcement> _announcements;
    private readonly IReadRepository<AnnouncementReceipt> _receipts;
    private readonly IReadRepository<Domain.Eis.Employee> _employees;
    private readonly ITenantContext _tenantContext;

    public GetAnnouncementReceiptsReportQueryHandler(
        IReadRepository<Announcement> announcements, IReadRepository<AnnouncementReceipt> receipts,
        IReadRepository<Domain.Eis.Employee> employees, ITenantContext tenantContext)
    {
        _announcements = announcements;
        _receipts = receipts;
        _employees = employees;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AnnouncementReceiptsReportDto>> Handle(
        GetAnnouncementReceiptsReportQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var announcementId = new AnnouncementId(request.AnnouncementId);

        var announcement = await _announcements.FirstOrDefaultAsync(
            new AnnouncementByIdSpecification(tenantId, announcementId), cancellationToken);
        if (announcement is null)
        {
            return Result.Failure<AnnouncementReceiptsReportDto>(Error.NotFound("announcement.not_found", "Announcement not found."));
        }

        var employees = await _employees.ListAsync(new EmployeesByTenantSpecification(tenantId), cancellationToken);
        var targetEmployees = employees.Where(e => AnnouncementAudienceMatcher.Matches(announcement, e)).ToList();

        var receipts = await _receipts.ListAsync(
            new AnnouncementReceiptsByAnnouncementSpecification(tenantId, announcementId), cancellationToken);
        var acknowledgedByEmployee = receipts
            .Where(r => r.AcknowledgedAt is not null)
            .ToDictionary(r => r.EmployeeId, r => r.AcknowledgedAt);

        var rows = targetEmployees
            .Select(e => new AnnouncementReceiptRowDto(
                e.Id.Value, $"{e.FirstName} {e.LastName}", acknowledgedByEmployee.ContainsKey(e.Id), acknowledgedByEmployee.GetValueOrDefault(e.Id)))
            .OrderBy(row => row.EmployeeName)
            .ToList();

        return Result.Success(new AnnouncementReceiptsReportDto(
            announcement.Id.Value, announcement.Title, targetEmployees.Count, rows.Count(r => r.Acknowledged), rows));
    }
}
