using Mapster;
using Vespera.Domain.Attendance;

namespace Vespera.Application.Features.Attendance;

public sealed class BiometricDeviceMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BiometricDevice, BiometricDeviceDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.LocationId, src => src.LocationId.Value)
            .Map(dest => dest.VendorType, src => src.VendorType.ToString());

        config.NewConfig<QuarantinedBiometricPunch, QuarantinedBiometricPunchDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.BiometricDeviceId, src => src.BiometricDeviceId.Value)
            .Map(dest => dest.PunchType, src => src.PunchType == null ? null : src.PunchType.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString());
    }
}
