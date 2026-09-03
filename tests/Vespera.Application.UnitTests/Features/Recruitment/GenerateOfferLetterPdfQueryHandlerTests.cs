using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GenerateOfferLetterPdfQueryHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetters = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly IReadRepository<Candidate> _candidates = Substitute.For<IReadRepository<Candidate>>();
    private readonly IPdfGenerator _pdfGenerator = Substitute.For<IPdfGenerator>();
    private readonly TenantId _tenantId = TenantId.New();

    private GenerateOfferLetterPdfQueryHandler CreateHandler() => new(_offerLetters, _candidates, _pdfGenerator);

    private OfferLetter CreateOfferForCandidate(CandidateId candidateId)
    {
        var offer = OfferLetter.Draft(_tenantId, candidateId, DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        return offer;
    }

    [Fact]
    public async Task Handle_Should_Generate_A_Pdf_Using_The_Candidates_Full_Name()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = CreateOfferForCandidate(candidate.Id);
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        var expectedBytes = new byte[] { 1, 2, 3 };
        _pdfGenerator.GenerateAsync(Arg.Any<PdfGenerationRequest>(), Arg.Any<CancellationToken>()).Returns(expectedBytes);

        var result = await CreateHandler().Handle(new GenerateOfferLetterPdfQuery(offer.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedBytes);
        await _pdfGenerator.Received(1).GenerateAsync(
            Arg.Is<PdfGenerationRequest>(r => r.TemplateKey == "OfferLetter" && (string)r.Data["Candidate"]! == "Jordan Lee"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fall_Back_To_The_Candidate_Id_When_The_Candidate_Is_Not_Found()
    {
        var candidateId = CandidateId.New();
        var offer = CreateOfferForCandidate(candidateId);
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);
        _pdfGenerator.GenerateAsync(Arg.Any<PdfGenerationRequest>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GenerateOfferLetterPdfQuery(offer.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _pdfGenerator.Received(1).GenerateAsync(
            Arg.Is<PdfGenerationRequest>(r => (string)r.Data["Candidate"]! == candidateId.Value.ToString()), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Offer_Not_Found()
    {
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((OfferLetter?)null);

        var result = await CreateHandler().Handle(new GenerateOfferLetterPdfQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
