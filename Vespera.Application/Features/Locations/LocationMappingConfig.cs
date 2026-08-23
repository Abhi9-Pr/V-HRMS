using Mapster;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Locations;

public sealed class LocationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Location, LocationDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Latitude, src => src.Coordinate.Latitude)
            .Map(dest => dest.Longitude, src => src.Coordinate.Longitude);

        config.NewConfig<Location, LocationSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
