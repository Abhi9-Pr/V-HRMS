using Mapster;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class TicketCategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<TicketCategory, TicketCategoryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.DepartmentId, src => src.DepartmentId.Value)
            .Map(dest => dest.DefaultSlaPolicyId, src => src.DefaultSlaPolicyId != null ? src.DefaultSlaPolicyId.Value.Value : (Guid?)null);
    }
}
