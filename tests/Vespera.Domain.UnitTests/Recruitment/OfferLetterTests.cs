using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.Recruitment.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Recruitment;

public class OfferLetterTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Accept_Should_Fail_Before_The_Offer_Is_Sent()
    {
        var offer = CreateOffer();

        var result = offer.Accept(Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Accept_After_Send_Should_Raise_OfferAccepted()
    {
        var offer = CreateOffer();
        offer.Send();

        var result = offer.Accept(Now);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Accepted);
        offer.DomainEvents.Should().ContainSingle(e => e is OfferAccepted);
    }

    [Fact]
    public void Withdraw_Should_Fail_Once_Accepted()
    {
        var offer = CreateOffer();
        offer.Send();
        offer.Accept(Now);

        var result = offer.Withdraw();

        result.IsFailure.Should().BeTrue();
    }

    private static OfferLetter CreateOffer() =>
        OfferLetter.Draft(
            TenantId.New(), CandidateId.New(), DesignationId.New(), Money.Of(1_200_000m, Currency.Inr), new DateOnly(2026, 3, 1));
}
