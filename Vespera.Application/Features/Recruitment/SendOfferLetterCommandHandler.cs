using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class SendOfferLetterCommandHandler : IRequestHandler<SendOfferLetterCommand, Result>
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads;
    private readonly IWriteRepository<OfferLetter> _offerLetters;
    private readonly IReadRepository<Candidate> _candidateReads;
    private readonly IWriteRepository<Candidate> _candidates;
    private readonly INotificationDispatcher _notificationDispatcher;

    public SendOfferLetterCommandHandler(
        IReadRepository<OfferLetter> offerLetterReads, IWriteRepository<OfferLetter> offerLetters, IReadRepository<Candidate> candidateReads,
        IWriteRepository<Candidate> candidates, INotificationDispatcher notificationDispatcher)
    {
        _offerLetterReads = offerLetterReads;
        _offerLetters = offerLetters;
        _candidateReads = candidateReads;
        _candidates = candidates;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(SendOfferLetterCommand request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetterReads.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        var candidate = await _candidateReads.FirstOrDefaultAsync(new CandidateByIdSpecification(offer.CandidateId), cancellationToken);
        if (candidate is null)
        {
            return Result.Failure(Error.NotFound("offer_letter.candidate_not_found", "The offer's candidate was not found."));
        }

        var sendResult = offer.Send();
        if (sendResult.IsFailure)
        {
            return sendResult;
        }

        var offerResult = candidate.MarkOffered();
        if (offerResult.IsFailure)
        {
            return offerResult;
        }

        _offerLetters.Update(offer);
        _candidates.Update(candidate);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                candidate.Id.Value.ToString(), "Your offer letter is ready",
                $"An offer of {offer.ProposedCtc} has been sent for your review.",
                new Dictionary<string, string> { ["offerLetterId"] = offer.Id.Value.ToString() }),
            cancellationToken);

        return Result.Success();
    }
}
