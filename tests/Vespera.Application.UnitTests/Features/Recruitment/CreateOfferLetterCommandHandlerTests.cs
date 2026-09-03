using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateOfferLetterCommandHandlerTests
{
    private readonly IReadRepository<Candidate> _candidates = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<OfferLetter> _offerLetters = Substitute.For<IWriteRepository<OfferLetter>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public CreateOfferLetterCommandHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private CreateOfferLetterCommandHandler CreateHandler() => new(_candidates, _offerLetters, _tenantContext);

    private Candidate CreateCandidate()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        return candidate;
    }

    [Fact]
    public async Task Handle_Should_Draft_An_Offer_Letter_For_The_Candidate()
    {
        var candidate = CreateCandidate();

        var result = await CreateHandler().Handle(
            new CreateOfferLetterCommand(candidate.Id.Value, Guid.NewGuid(), 1200000m, Currency.Inr, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _offerLetters.Received(1).AddAsync(
            Arg.Is<OfferLetter>(o => o.CandidateId == candidate.Id && o.Status == OfferLetterStatus.Draft), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Candidate_Not_Found()
    {
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);

        var result = await CreateHandler().Handle(
            new CreateOfferLetterCommand(Guid.NewGuid(), Guid.NewGuid(), 1200000m, Currency.Inr, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
