using Mapster;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Designations;

public sealed class DesignationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Designation, DesignationDto>()
            .Map(dest => dest.Id, src => src.Id.Value);

        config.NewConfig<Designation, DesignationSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
