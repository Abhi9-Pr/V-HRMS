using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result>
{
    private readonly IReadRepository<Location> _locations;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteLocationCommandHandler(
        IReadRepository<Location> locations,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _locations = locations;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        var specification = new LocationByIdSpecification(_tenantContext.TenantId, new LocationId(request.Id));

        var location = await _locations.FirstOrDefaultAsync(specification, cancellationToken);
        if (location is null)
        {
            return Result.Failure(Error.NotFound("location.not_found", "Location not found."));
        }

        return location.Delete(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
