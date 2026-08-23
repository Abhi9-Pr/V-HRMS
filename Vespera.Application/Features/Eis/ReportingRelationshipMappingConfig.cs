using Mapster;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class ReportingRelationshipMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ReportingRelationship, ReportingRelationshipDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.EmployeeId, src => src.EmployeeId.Value)
            .Map(dest => dest.ManagerId, src => src.ManagerId.Value);
    }
}
