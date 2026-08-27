using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class RespondToEventRsvpCommandHandler : IRequestHandler<RespondToEventRsvpCommand, Result>
{
    private readonly IReadRepository<EventRsvp> _rsvps;
    private readonly IWriteRepository<EventRsvp> _rsvpWriter;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public RespondToEventRsvpCommandHandler(
        IReadRepository<EventRsvp> rsvps, IWriteRepository<EventRsvp> rsvpWriter, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _rsvps = rsvps;
        _rsvpWriter = rsvpWriter;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(RespondToEventRsvpCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure(Error.Validation("event_rsvp.no_employee", "The signed-in account is not linked to an employee."));
        }

        var tenantId = _tenantContext.TenantId;
        var corporateEventId = new CorporateEventId(request.CorporateEventId);

        var rsvp = await _rsvps.FirstOrDefaultAsync(
            new EventRsvpByEventAndEmployeeSpecification(tenantId, corporateEventId, employeeId.Value), cancellationToken);

        if (rsvp is null)
        {
            rsvp = EventRsvp.Create(tenantId, corporateEventId, employeeId.Value);
            rsvp.Respond(request.Response, _dateTimeProvider.UtcNow);
            await _rsvpWriter.AddAsync(rsvp, cancellationToken);
            return Result.Success();
        }

        rsvp.Respond(request.Response, _dateTimeProvider.UtcNow);
        _rsvpWriter.Update(rsvp);
        return Result.Success();
    }
}
