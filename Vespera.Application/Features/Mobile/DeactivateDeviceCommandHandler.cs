using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

/// <summary>Self-service only: a caller can only deactivate their own device registrations — the
/// lookup specification is scoped by tenant and id only, so ownership is checked explicitly
/// below rather than folded into the query, giving a clean 403 instead of a misleading 404 when
/// someone tries to deactivate a device that isn't theirs.</summary>
public sealed class DeactivateDeviceCommandHandler : IRequestHandler<DeactivateDeviceCommand, Result>
{
    private readonly IReadRepository<DeviceRegistration> _readRepository;
    private readonly IWriteRepository<DeviceRegistration> _writeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public DeactivateDeviceCommandHandler(
        IReadRepository<DeviceRegistration> readRepository,
        IWriteRepository<DeviceRegistration> writeRepository,
        ITenantContext tenantContext,
        ICurrentUser currentUser)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeactivateDeviceCommand request, CancellationToken cancellationToken)
    {
        var registration = await _readRepository.FirstOrDefaultAsync(
            new DeviceRegistrationByIdSpecification(_tenantContext.TenantId, new DeviceRegistrationId(request.DeviceRegistrationId)),
            cancellationToken);

        if (registration is null)
        {
            return Result.Failure(Error.NotFound("device_registration.not_found", "No such device registration."));
        }

        if (_currentUser.UserId is not { } userIdValue || registration.UserId.Value != userIdValue)
        {
            return Result.Failure(Error.Forbidden("device_registration.not_owner", "Not this device's owner."));
        }

        var result = registration.Deactivate();
        if (result.IsFailure)
        {
            return result;
        }

        _writeRepository.Update(registration);
        return Result.Success();
    }
}
