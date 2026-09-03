using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetCandidateByIdQueryHandlerTests
{
    private readonly IReadRepository<Candidate> _candidates = Substitute.For<IReadRepository<Candidate>>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetCandidateByIdQueryHandler CreateHandler() => new(_candidates);

    [Fact]
    public async Task Handle_Should_Return_The_Candidate_Dto()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);

        var result = await CreateHandler().Handle(new GetCandidateByIdQuery(candidate.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(candidate.Id.Value);
        result.Value.FullName.Should().Be("Jordan Lee");
        result.Value.Email.Should().Be("jordan.lee@example.com");
        result.Value.Phone.Should().Be("+14155552671");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Candidate_Not_Found()
    {
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);

        var result = await CreateHandler().Handle(new GetCandidateByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
