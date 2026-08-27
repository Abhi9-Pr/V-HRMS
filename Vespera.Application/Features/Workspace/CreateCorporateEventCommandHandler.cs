using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateCorporateEventCommandHandler : IRequestHandler<CreateCorporateEventCommand, Result<Guid>>
{
    private readonly IWriteRepository<CorporateEvent> _events;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateCorporateEventCommandHandler(
        IWriteRepository<CorporateEvent> events, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _events = events;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateCorporateEventCommand request, CancellationToken cancellationToken)
    {
        var result = CorporateEvent.Create(
            _tenantContext.TenantId, request.Title, request.Description, request.StartsAt, request.EndsAt, request.LocationText,
            _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _events.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
