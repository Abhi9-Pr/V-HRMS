using Mapster;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class CandidateMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Candidate, CandidateCardDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CurrentPipelineStageId, src => src.CurrentPipelineStageId != null ? src.CurrentPipelineStageId.Value.Value : (Guid?)null);

        config.NewConfig<Candidate, CandidateDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.JobRequisitionId, src => src.JobRequisitionId.Value)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.Phone, src => src.Phone.Value)
            .Map(dest => dest.CurrentPipelineStageId, src => src.CurrentPipelineStageId != null ? src.CurrentPipelineStageId.Value.Value : (Guid?)null);
    }
}
