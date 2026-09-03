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

public class SendOfferLetterCommandHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly IWriteRepository<OfferLetter> _offerLetters = Substitute.For<IWriteRepository<OfferLetter>>();
    private readonly IReadRepository<Candidate> _candidateReads = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();

    private SendOfferLetterCommandHandler CreateHandler() =>
        new(_offerLetterReads, _offerLetters, _candidateReads, _candidates, _notificationDispatcher);

    private (OfferLetter Offer, Candidate Candidate) CreateDraftOfferWithCandidate()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = OfferLetter.Draft(_tenantId, candidate.Id, DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));

        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);

        return (offer, candidate);
    }

    [Fact]
    public async Task Handle_Should_Send_The_Offer_And_Mark_The_Candidate_Offered()
    {
        var (offer, candidate) = CreateDraftOfferWithCandidate();

        var result = await CreateHandler().Handle(new SendOfferLetterCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Sent);
        candidate.Status.Should().Be(CandidateStatus.Offered);
        _offerLetters.Received(1).Update(offer);
        _candidates.Received(1).Update(candidate);
        await _notificationDispatcher.Received(1).DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Offer_Not_Found()
    {
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((OfferLetter?)null);

        var result = await CreateHandler().Handle(new SendOfferLetterCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Offer_Is_Not_A_Draft()
    {
        var (offer, _) = CreateDraftOfferWithCandidate();
        offer.Send();

        var result = await CreateHandler().Handle(new SendOfferLetterCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
