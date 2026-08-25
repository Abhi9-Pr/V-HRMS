using Mapster;
using Vespera.Domain.Attendance;

namespace Vespera.Application.Features.Holidays;

public sealed class HolidayMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Holiday, HolidayDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.LocationId, src => src.LocationId.Value);

        config.NewConfig<Holiday, HolidaySummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
