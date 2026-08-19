using Mapster;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Departments;

public sealed class DepartmentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Department, DepartmentDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ParentDepartmentId, src => src.ParentDepartmentId != null ? src.ParentDepartmentId.Value.Value : (Guid?)null);

        config.NewConfig<Department, DepartmentSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
