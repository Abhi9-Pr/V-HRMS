using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class GetUpcomingCorporateEventsQueryHandler
    : IRequestHandler<GetUpcomingCorporateEventsQuery, Result<IReadOnlyList<CorporateEventSummaryDto>>>
{
    private readonly IReadRepository<CorporateEvent> _events;
    private readonly IReadRepository<EventRsvp> _rsvps;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetUpcomingCorporateEventsQueryHandler(
        IReadRepository<CorporateEvent> events, IReadRepository<EventRsvp> rsvps, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _events = events;
        _rsvps = rsvps;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<IReadOnlyList<CorporateEventSummaryDto>>> Handle(
        GetUpcomingCorporateEventsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var events = await _events.ListAsync(
            new UpcomingCorporateEventsSpecification(tenantId, _dateTimeProvider.UtcNow), cancellationToken);

        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        var myResponses = employeeId is null
            ? []
            : (await _rsvps.ListAsync(new EventRsvpsByEmployeeSpecification(tenantId, employeeId.Value), cancellationToken))
                .ToDictionary(r => r.CorporateEventId, r => r.Response);

        var items = events
            .Select(e => new CorporateEventSummaryDto(
                e.Id.Value, e.Title, e.Description, e.StartsAt, e.EndsAt, e.LocationText,
                myResponses.TryGetValue(e.Id, out var response) ? response?.ToString() : null))
            .ToList();

        return Result.Success<IReadOnlyList<CorporateEventSummaryDto>>(items);
    }
}
