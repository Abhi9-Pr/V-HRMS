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

    [Fact]
    public void Send_Should_Succeed_From_Draft()
    {
        var offer = CreateOffer();

        var result = offer.Send();

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Sent);
    }

    [Fact]
    public void Send_Should_Fail_When_Not_Draft()
    {
        var offer = CreateOffer();
        offer.Send();

        var result = offer.Send();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offer_letter.not_draft");
    }

    [Fact]
    public void Decline_Should_Succeed_After_Send()
    {
        var offer = CreateOffer();
        offer.Send();

        var result = offer.Decline();

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Declined);
    }

    [Fact]
    public void Decline_Should_Fail_Before_Send()
    {
        var offer = CreateOffer();

        var result = offer.Decline();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offer_letter.not_sent");
    }

    [Fact]
    public void Withdraw_Should_Succeed_While_Draft()
    {
        var offer = CreateOffer();

        var result = offer.Withdraw();

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Withdrawn);
    }

    [Fact]
    public void OfferLetterId_New_Should_Generate_Distinct_Values()
    {
        OfferLetterId.New().Should().NotBe(OfferLetterId.New());
    }

    private static OfferLetter CreateOffer() =>
        OfferLetter.Draft(
            TenantId.New(), CandidateId.New(), DesignationId.New(), Money.Of(1_200_000m, Currency.Inr), new DateOnly(2026, 3, 1));
}
