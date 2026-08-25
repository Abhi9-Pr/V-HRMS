using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Attendance;

public sealed class RegisterBiometricDeviceCommandHandler : IRequestHandler<RegisterBiometricDeviceCommand, Result<Guid>>
{
    private readonly IWriteRepository<BiometricDevice> _devices;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterBiometricDeviceCommandHandler(
        IWriteRepository<BiometricDevice> devices,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _devices = devices;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(RegisterBiometricDeviceCommand request, CancellationToken cancellationToken)
    {
        var vendorType = Enum.Parse<BiometricVendorType>(request.VendorType, ignoreCase: true);

        var result = BiometricDevice.Register(
            _tenantContext.TenantId,
            new LocationId(request.LocationId),
            vendorType,
            request.Host,
            request.Port,
            request.ApiKeyConfigurationKey,
            _dateTimeProvider.UtcNow,
            _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _devices.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
