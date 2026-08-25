using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Assets;
using Vespera.Domain.Eis.Events;

namespace Vespera.Application.Features.Assets;

/// <summary>
/// Reacts to an employee exit by releasing every software-license seat they hold (freeing it up
/// for the unused-seat report) and starting asset recovery for every asset still assigned to
/// them. This is the first consumer of <see cref="DomainEventNotification{TDomainEvent}"/> in the
/// codebase: it runs out-of-band, replayed by the outbox dispatcher from a background scope with
/// no HTTP request, so the ambient tenant global query filter (see <c>HttpTenantContext</c>) is
/// always "no tenant" here — every read goes through <see cref="IReadRepositoryAdmin{T}"/> (the
/// documented filter-bypassing escape hatch) with an explicit <c>TenantId</c> equality check taken
/// directly off the event instead, rather than the plain <see cref="IReadRepository{T}"/> every
/// other handler in this codebase safely relies on the ambient filter for.
/// </summary>
public sealed class EmployeeExitedNotificationHandler : INotificationHandler<DomainEventNotification<EmployeeExited>>
{
    private readonly IReadRepositoryAdmin<SoftwareLicenseAllocation> _allocationReads;
    private readonly IReadRepositoryAdmin<SoftwareLicense> _licenses;
    private readonly IReadRepositoryAdmin<AssetAssignment> _assignments;
    private readonly IWriteRepository<AssetRecovery> _recoveryWrites;
    private readonly IReadRepositoryAdmin<OffboardingChecklist> _checklistReads;
    private readonly IWriteRepository<OffboardingChecklist> _checklistWrites;
    private readonly IDateTimeProvider _dateTimeProvider;

    public EmployeeExitedNotificationHandler(
        IReadRepositoryAdmin<SoftwareLicenseAllocation> allocationReads,
        IReadRepositoryAdmin<SoftwareLicense> licenses,
        IReadRepositoryAdmin<AssetAssignment> assignments,
        IWriteRepository<AssetRecovery> recoveryWrites,
        IReadRepositoryAdmin<OffboardingChecklist> checklistReads,
        IWriteRepository<OffboardingChecklist> checklistWrites,
        IDateTimeProvider dateTimeProvider)
    {
        _allocationReads = allocationReads;
        _licenses = licenses;
        _assignments = assignments;
        _recoveryWrites = recoveryWrites;
        _checklistReads = checklistReads;
        _checklistWrites = checklistWrites;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DomainEventNotification<EmployeeExited> notification, CancellationToken cancellationToken)
    {
        var employeeId = notification.DomainEvent.EmployeeId;
        var tenantId = notification.DomainEvent.TenantId;
        var now = _dateTimeProvider.UtcNow;

        var activeAllocations = await _allocationReads.ListIgnoringFiltersAsync(
            new ActiveSoftwareLicenseAllocationsByEmployeeSpecification(tenantId, employeeId), cancellationToken);

        foreach (var allocation in activeAllocations)
        {
            if (allocation.Release(now).IsFailure)
            {
                continue;
            }

            var licenses = await _licenses.ListIgnoringFiltersAsync(
                new SoftwareLicenseByIdSpecification(allocation.LicenseId), cancellationToken);
            if (licenses.Count > 0)
            {
                licenses[0].ReleaseSeat();
            }
        }

        var activeAssignments = await _assignments.ListIgnoringFiltersAsync(
            new ActiveAssetAssignmentsByEmployeeSpecification(tenantId, employeeId), cancellationToken);

        if (activeAssignments.Count == 0)
        {
            return;
        }

        var existingChecklists = await _checklistReads.ListIgnoringFiltersAsync(
            new OffboardingChecklistByEmployeeSpecification(tenantId, employeeId), cancellationToken);

        // A checklist is created once, seeded with one "recover this asset" item per asset still
        // assigned at the moment of exit. If one already exists (e.g. HR started offboarding
        // paperwork before the exit was recorded) it's left as-is rather than diffed item-by-item
        // — a deliberate simplification, not an oversight.
        if (existingChecklists.Count == 0)
        {
            var checklist = OffboardingChecklist.Create(
                tenantId, employeeId, activeAssignments.Select(assignment => $"Recover asset {assignment.AssetId.Value}"));
            await _checklistWrites.AddAsync(checklist, cancellationToken);
        }

        foreach (var assignment in activeAssignments)
        {
            var recovery = AssetRecovery.Initiate(tenantId, assignment.Id, assignment.AssetId, employeeId, now);
            await _recoveryWrites.AddAsync(recovery, cancellationToken);
        }
    }
}
