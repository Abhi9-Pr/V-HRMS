using Mapster;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class InterviewMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Interview, InterviewDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CandidateId, src => src.CandidateId.Value)
            .Map(dest => dest.PipelineStageId, src => src.PipelineStageId.Value)
            .Map(dest => dest.InterviewerIds, src => src.InterviewerIds.Select(id => id.Value).ToList());

        config.NewConfig<InterviewScorecard, InterviewScorecardDto>()
            .Map(dest => dest.InterviewerId, src => src.InterviewerId.Value);
    }
}
