using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

/// <summary>Upserts by (tenant, caller, device id): a device the caller already registered gets
/// its push token refreshed and is reactivated if it had been deactivated; a new device id gets a
/// new <see cref="DeviceRegistration"/>. This is what makes calling <c>register</c> on every app
/// launch safe — it never accumulates duplicate rows for the same physical device.</summary>
public sealed class RegisterDeviceCommandHandler : IRequestHandler<RegisterDeviceCommand, Result<Guid>>
{
    private readonly IReadRepository<DeviceRegistration> _readRepository;
    private readonly IWriteRepository<DeviceRegistration> _writeRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public RegisterDeviceCommandHandler(
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

    public async Task<Result<Guid>> Handle(RegisterDeviceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return Result.Failure<Guid>(Error.Unauthorized("device_registration.not_authenticated", "Not authenticated."));
        }

        var tenantId = _tenantContext.TenantId;
        var userId = new UserId(userIdValue);

        var existing = await _readRepository.FirstOrDefaultAsync(
            new DeviceRegistrationByUserAndDeviceIdSpecification(tenantId, userId, request.DeviceId), cancellationToken);

        if (existing is not null)
        {
            var updateResult = existing.UpdatePushToken(request.PushToken);
            if (updateResult.IsFailure)
            {
                return Result.Failure<Guid>(updateResult.Error);
            }

            if (!existing.IsActive)
            {
                existing.Reactivate();
            }

            _writeRepository.Update(existing);
            return Result.Success(existing.Id.Value);
        }

        var registerResult = DeviceRegistration.Register(tenantId, userId, request.DeviceId, request.Platform, request.PushToken);
        if (registerResult.IsFailure)
        {
            return Result.Failure<Guid>(registerResult.Error);
        }

        await _writeRepository.AddAsync(registerResult.Value, cancellationToken);
        return Result.Success(registerResult.Value.Id.Value);
    }
}
