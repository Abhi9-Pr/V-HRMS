using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CancelCorporateEventCommandHandler : IRequestHandler<CancelCorporateEventCommand, Result>
{
    private readonly IReadRepository<CorporateEvent> _events;
    private readonly IWriteRepository<CorporateEvent> _eventWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CancelCorporateEventCommandHandler(
        IReadRepository<CorporateEvent> events, IWriteRepository<CorporateEvent> eventWriter, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _events = events;
        _eventWriter = eventWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(CancelCorporateEventCommand request, CancellationToken cancellationToken)
    {
        var corporateEvent = await _events.FirstOrDefaultAsync(
            new CorporateEventByIdSpecification(_tenantContext.TenantId, new CorporateEventId(request.CorporateEventId)), cancellationToken);
        if (corporateEvent is null)
        {
            return Result.Failure(Error.NotFound("corporate_event.not_found", "Event not found."));
        }

        var result = corporateEvent.Cancel(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _eventWriter.Update(corporateEvent);
        return Result.Success();
    }
}
