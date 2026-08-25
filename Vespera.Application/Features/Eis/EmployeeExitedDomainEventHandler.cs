using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Eis.Events;

namespace Vespera.Application.Features.Eis;

/// <summary>
/// Initiates an <see cref="OffboardingChecklist"/> the moment an employee exits. Domain events
/// reach here asynchronously, via the outbox (see <c>OutboxDispatcherHostedService</c>) — at-least-once
/// delivery, so this checks whether a checklist already exists for the employee before creating
/// one, rather than assuming it will only ever run once per exit.
/// </summary>
public sealed class EmployeeExitedDomainEventHandler : INotificationHandler<DomainEventNotification<EmployeeExited>>
{
    private readonly IReadRepositoryAdmin<OffboardingChecklist> _checklistsAdmin;
    private readonly IWriteRepository<OffboardingChecklist> _checklistWriter;

    public EmployeeExitedDomainEventHandler(
        IReadRepositoryAdmin<OffboardingChecklist> checklistsAdmin, IWriteRepository<OffboardingChecklist> checklistWriter)
    {
        _checklistsAdmin = checklistsAdmin;
        _checklistWriter = checklistWriter;
    }

    public async Task Handle(DomainEventNotification<EmployeeExited> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var existing = await _checklistsAdmin.ListIgnoringFiltersAsync(
            new OffboardingChecklistByEmployeeIdSpecification(domainEvent.TenantId, domainEvent.EmployeeId), cancellationToken);
        if (existing.Count > 0)
        {
            return;
        }

        var checklistResult = OffboardingChecklist.Initiate(
            domainEvent.TenantId, domainEvent.EmployeeId, domainEvent.ExitDate, domainEvent.OccurredOn, "system");
        if (checklistResult.IsFailure)
        {
            return;
        }

        await _checklistWriter.AddAsync(checklistResult.Value, cancellationToken);
    }
}
