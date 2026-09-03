using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetOfferLetterByIdQueryHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetters = Substitute.For<IReadRepository<OfferLetter>>();

    private GetOfferLetterByIdQueryHandler CreateHandler() => new(_offerLetters);

    [Fact]
    public async Task Handle_Should_Return_The_Offer_Letter_Dto()
    {
        var offer = OfferLetter.Draft(
            TenantId.New(), CandidateId.New(), DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);

        var result = await CreateHandler().Handle(new GetOfferLetterByIdQuery(offer.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(offer.Id.Value);
        result.Value.ProposedCtc.Should().Be(1200000m);
        result.Value.Currency.Should().Be("Inr");
        result.Value.Status.Should().Be(OfferLetterStatus.Draft);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Offer_Not_Found()
    {
        _offerLetters.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((OfferLetter?)null);

        var result = await CreateHandler().Handle(new GetOfferLetterByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
