using Mapster;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class OfferLetterMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OfferLetter, OfferLetterDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CandidateId, src => src.CandidateId.Value)
            .Map(dest => dest.ProposedDesignationId, src => src.ProposedDesignationId.Value)
            .Map(dest => dest.ProposedCtc, src => src.ProposedCtc.Amount)
            .Map(dest => dest.Currency, src => src.ProposedCtc.Currency.ToString());
    }
}
