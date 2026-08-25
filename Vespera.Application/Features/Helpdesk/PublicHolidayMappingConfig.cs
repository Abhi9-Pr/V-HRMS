using Mapster;
using Vespera.Domain.Attendance;

namespace Vespera.Application.Features.Helpdesk;

public sealed class PublicHolidayMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PublicHoliday, PublicHolidayDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
