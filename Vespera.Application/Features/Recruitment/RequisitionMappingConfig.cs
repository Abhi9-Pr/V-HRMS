using Mapster;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class RequisitionMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<JobRequisition, JobRequisitionDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.DepartmentId, src => src.DepartmentId.Value);

        config.NewConfig<PipelineStage, PipelineStageDto>()
            .Map(dest => dest.Id, src => src.Id.Value);
    }
}
