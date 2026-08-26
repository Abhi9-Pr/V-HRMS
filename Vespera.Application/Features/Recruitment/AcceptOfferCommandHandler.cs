using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class AcceptOfferCommandHandler : IRequestHandler<AcceptOfferCommand, Result>
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads;
    private readonly IWriteRepository<OfferLetter> _offerLetters;
    private readonly IReadRepository<Candidate> _candidateReads;
    private readonly IWriteRepository<Candidate> _candidates;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AcceptOfferCommandHandler(
        IReadRepository<OfferLetter> offerLetterReads, IWriteRepository<OfferLetter> offerLetters, IReadRepository<Candidate> candidateReads,
        IWriteRepository<Candidate> candidates, IDateTimeProvider dateTimeProvider)
    {
        _offerLetterReads = offerLetterReads;
        _offerLetters = offerLetters;
        _candidateReads = candidateReads;
        _candidates = candidates;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetterReads.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        var acceptResult = offer.Accept(_dateTimeProvider.UtcNow);
        if (acceptResult.IsFailure)
        {
            return acceptResult;
        }

        _offerLetters.Update(offer);

        var candidate = await _candidateReads.FirstOrDefaultAsync(new CandidateByIdSpecification(offer.CandidateId), cancellationToken);
        if (candidate is not null && candidate.Status != CandidateStatus.Hired)
        {
            candidate.MarkHired();
            _candidates.Update(candidate);
        }

        return Result.Success();
    }
}
