using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

/// <summary>Declining an offer does not, by itself, change the candidate's pipeline status — HR
/// decides the candidate's next step separately (reject, withdraw, or move them into a different
/// stage for a revised offer).</summary>
public sealed class DeclineOfferCommandHandler : IRequestHandler<DeclineOfferCommand, Result>
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads;
    private readonly IWriteRepository<OfferLetter> _offerLetters;

    public DeclineOfferCommandHandler(IReadRepository<OfferLetter> offerLetterReads, IWriteRepository<OfferLetter> offerLetters)
    {
        _offerLetterReads = offerLetterReads;
        _offerLetters = offerLetters;
    }

    public async Task<Result> Handle(DeclineOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetterReads.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        var result = offer.Decline();
        if (result.IsFailure)
        {
            return result;
        }

        _offerLetters.Update(offer);
        return Result.Success();
    }
}
