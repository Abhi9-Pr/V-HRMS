using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GetOfferLetterByIdQueryHandler : IRequestHandler<GetOfferLetterByIdQuery, Result<OfferLetterDto>>
{
    private readonly IReadRepository<OfferLetter> _offerLetters;

    public GetOfferLetterByIdQueryHandler(IReadRepository<OfferLetter> offerLetters)
    {
        _offerLetters = offerLetters;
    }

    public async Task<Result<OfferLetterDto>> Handle(GetOfferLetterByIdQuery request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetters.FirstOrDefaultAsync(new OfferLetterByIdSpecification(new OfferLetterId(request.Id)), cancellationToken);

        return offer is null
            ? Result.Failure<OfferLetterDto>(Error.NotFound("offer_letter.not_found", "Offer letter not found."))
            : Result.Success(offer.Adapt<OfferLetterDto>());
    }
}
