using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class WithdrawOfferCommandHandler : IRequestHandler<WithdrawOfferCommand, Result>
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads;
    private readonly IWriteRepository<OfferLetter> _offerLetters;

    public WithdrawOfferCommandHandler(IReadRepository<OfferLetter> offerLetterReads, IWriteRepository<OfferLetter> offerLetters)
    {
        _offerLetterReads = offerLetterReads;
        _offerLetters = offerLetters;
    }

    public async Task<Result> Handle(WithdrawOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetterReads.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        var result = offer.Withdraw();
        if (result.IsFailure)
        {
            return result;
        }

        _offerLetters.Update(offer);
        return Result.Success();
    }
}
