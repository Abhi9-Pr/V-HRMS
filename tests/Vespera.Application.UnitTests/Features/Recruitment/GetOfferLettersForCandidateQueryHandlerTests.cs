using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetOfferLettersForCandidateQueryHandlerTests
{
    private readonly IReadRepository<OfferLetter> _offerLetters = Substitute.For<IReadRepository<OfferLetter>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetOfferLettersForCandidateQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetOfferLettersForCandidateQueryHandler CreateHandler() => new(_offerLetters, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Offers_For_The_Candidate()
    {
        var candidateId = CandidateId.New();
        var offer = OfferLetter.Draft(_tenantId, candidateId, DesignationId.New(), Money.Of(1200000m, Currency.Inr), new DateOnly(2026, 6, 1));
        _offerLetters.ListAsync(Arg.Any<OfferLettersByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([offer]);

        var result = await CreateHandler().Handle(new GetOfferLettersForCandidateQuery(candidateId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == offer.Id.Value && dto.CandidateId == candidateId.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_Offers()
    {
        _offerLetters.ListAsync(Arg.Any<OfferLettersByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetOfferLettersForCandidateQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
