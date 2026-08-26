using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed class GenerateOfferLetterPdfQueryHandler : IRequestHandler<GenerateOfferLetterPdfQuery, Result<byte[]>>
{
    private readonly IReadRepository<OfferLetter> _offerLetters;
    private readonly IReadRepository<Candidate> _candidates;
    private readonly IPdfGenerator _pdfGenerator;

    public GenerateOfferLetterPdfQueryHandler(
        IReadRepository<OfferLetter> offerLetters, IReadRepository<Candidate> candidates, IPdfGenerator pdfGenerator)
    {
        _offerLetters = offerLetters;
        _candidates = candidates;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<Result<byte[]>> Handle(GenerateOfferLetterPdfQuery request, CancellationToken cancellationToken)
    {
        var offer = await _offerLetters.FirstOrDefaultAsync(
            new OfferLetterByIdSpecification(new OfferLetterId(request.OfferLetterId)), cancellationToken);

        if (offer is null)
        {
            return Result.Failure<byte[]>(Error.NotFound("offer_letter.not_found", "Offer letter not found."));
        }

        var candidate = await _candidates.FirstOrDefaultAsync(new CandidateByIdSpecification(offer.CandidateId), cancellationToken);

        var data = new Dictionary<string, object?>
        {
            ["Candidate"] = candidate?.FullName ?? offer.CandidateId.Value.ToString(),
            ["Proposed CTC"] = offer.ProposedCtc.ToString(),
            ["Joining Date"] = offer.JoiningDate.ToString("yyyy-MM-dd"),
            ["Status"] = offer.Status.ToString(),
        };

        var bytes = await _pdfGenerator.GenerateAsync(new PdfGenerationRequest("OfferLetter", data), cancellationToken);
        return Result.Success(bytes);
    }
}
