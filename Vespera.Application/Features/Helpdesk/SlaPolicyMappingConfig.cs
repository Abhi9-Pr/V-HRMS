using Mapster;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class SlaPolicyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SlaPolicy, SlaPolicyDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
