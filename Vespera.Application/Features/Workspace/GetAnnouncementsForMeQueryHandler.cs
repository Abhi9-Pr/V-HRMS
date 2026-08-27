using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using EmployeeByIdSpecification = Vespera.Application.Features.Employees.EmployeeByIdSpecification;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class GetAnnouncementsForMeQueryHandler
    : IRequestHandler<GetAnnouncementsForMeQuery, Result<IReadOnlyList<AnnouncementSummaryDto>>>
{
    private readonly IReadRepository<Announcement> _announcements;
    private readonly IReadRepository<AnnouncementReceipt> _receipts;
    private readonly IReadRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetAnnouncementsForMeQueryHandler(
        IReadRepository<Announcement> announcements, IReadRepository<AnnouncementReceipt> receipts, IReadRepository<Employee> employees,
        ITenantContext tenantContext, IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _announcements = announcements;
        _receipts = receipts;
        _employees = employees;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<IReadOnlyList<AnnouncementSummaryDto>>> Handle(
        GetAnnouncementsForMeQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<IReadOnlyList<AnnouncementSummaryDto>>([]);
        }

        var tenantId = _tenantContext.TenantId;
        var employee = await _employees.FirstOrDefaultAsync(new EmployeeByIdSpecification(tenantId, employeeId.Value), cancellationToken);
        if (employee is null)
        {
            return Result.Success<IReadOnlyList<AnnouncementSummaryDto>>([]);
        }

        var now = _dateTimeProvider.UtcNow;
        var candidates = await _announcements.ListAsync(new PublishedAnnouncementsSpecification(tenantId, now), cancellationToken);

        var forMe = candidates.Where(a => AnnouncementAudienceMatcher.Matches(a, employee)).ToList();

        var receipts = await _receipts.ListAsync(
            new AnnouncementReceiptsByEmployeeSpecification(tenantId, employeeId.Value), cancellationToken);
        var acknowledgedIds = receipts.Where(r => r.AcknowledgedAt is not null).Select(r => r.AnnouncementId).ToHashSet();

        var ordered = forMe
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => (int)a.Priority)
            .ThenByDescending(a => a.PublishAt)
            .Select(a => new AnnouncementSummaryDto(
                a.Id.Value, a.Title, a.Body, a.Priority.ToString(), a.IsPinned, a.PublishAt, acknowledgedIds.Contains(a.Id)))
            .ToList();

        return Result.Success<IReadOnlyList<AnnouncementSummaryDto>>(ordered);
    }
}
