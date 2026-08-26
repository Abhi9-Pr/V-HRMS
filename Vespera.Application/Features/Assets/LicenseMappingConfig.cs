using Mapster;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class LicenseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SoftwareLicense, SoftwareLicenseDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
