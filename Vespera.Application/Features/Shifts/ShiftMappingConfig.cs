using Mapster;
using Vespera.Domain.Attendance;

namespace Vespera.Application.Features.Shifts;

public sealed class ShiftMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Shift, ShiftDto>()
            .Map(dest => dest.Id, src => src.Id.Value);

        config.NewConfig<Shift, ShiftSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
