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

public class AcceptOfferCommandHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetterReads = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly IWriteRepository<OfferLetter> _offerLetters = Substitute.For<IWriteRepository<OfferLetter>>();
    private readonly IReadRepository<Candidate> _candidateReads = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private AcceptOfferCommandHandler CreateHandler() => new(_offerLetterReads, _offerLetters, _candidateReads, _candidates, _dateTimeProvider);

    private (OfferLetter Offer, Candidate Candidate) CreateSentOfferWithCandidate()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = OfferLetter.Draft(_tenantId, candidate.Id, DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        offer.Send();

        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        return (offer, candidate);
    }

    [Fact]
    public async Task Handle_Should_Accept_The_Offer_And_Mark_The_Candidate_Hired()
    {
        var (offer, candidate) = CreateSentOfferWithCandidate();

        var result = await CreateHandler().Handle(new AcceptOfferCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        offer.Status.Should().Be(OfferLetterStatus.Accepted);
        candidate.Status.Should().Be(CandidateStatus.Hired);
        _offerLetters.Received(1).Update(offer);
        _candidates.Received(1).Update(candidate);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Offer_Not_Found()
    {
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((OfferLetter?)null);

        var result = await CreateHandler().Handle(new AcceptOfferCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Offer_Was_Never_Sent()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        var offer = OfferLetter.Draft(_tenantId, candidate.Id, DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        _offerLetterReads.FirstOrDefaultAsync(Arg.Any<OfferLetterByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(offer);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new AcceptOfferCommand(offer.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _offerLetters.DidNotReceive().Update(Arg.Any<OfferLetter>());
    }
}
