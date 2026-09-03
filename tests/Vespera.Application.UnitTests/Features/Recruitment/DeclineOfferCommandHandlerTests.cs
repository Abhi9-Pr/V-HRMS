using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class DeclineOfferCommandHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly IWriteRepository<OfferLetter> _offerLetters = Substitute.For<IWriteRepository<OfferLetter>>();
    private readonly TenantId _tenantId = TenantId.New();

    private DeclineOfferCommandHandler CreateHandler() => new(_offerLetterReads, _offerLetters);

    private OfferLetter CreateSentOffer()
    {
        var offer = OfferLetter.Draft(_tenantId, CandidateId.New(), DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        offer.Send();
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        return offer;
    }

    [Fact]
    public async Task Handle_Should_Decline_A_Sent_Offer()
    {
        var offer = CreateSentOffer();

        var result = await CreateHandler().Handle(new DeclineOfferCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Declined);
        _offerLetters.Received(1).Update(offer);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Offer_Not_Found()
    {
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((OfferLetter?)null);

        var result = await CreateHandler().Handle(new DeclineOfferCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Offer_Was_Never_Sent()
    {
        var offer = OfferLetter.Draft(_tenantId, CandidateId.New(), DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);

        var result = await CreateHandler().Handle(new DeclineOfferCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _offerLetters.DidNotReceive().Update(Arg.Any<OfferLetter>());
    }
}
