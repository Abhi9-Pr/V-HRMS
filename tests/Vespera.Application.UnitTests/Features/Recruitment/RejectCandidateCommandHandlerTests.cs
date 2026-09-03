using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class RejectCandidateCommandHandlerTests
{
    private readonly IReadRepository<Candidate> _candidateReads = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly TenantId _tenantId = TenantId.New();

    private RejectCandidateCommandHandler CreateHandler() => new(_candidateReads, _candidates);

    private Candidate CreateCandidate()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        return candidate;
    }

    [Fact]
    public async Task Handle_Should_Reject_An_Active_Candidate()
    {
        var candidate = CreateCandidate();

        var result = await CreateHandler().Handle(new RejectCandidateCommand(candidate.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        candidate.Status.Should().Be(CandidateStatus.Rejected);
        _candidates.Received(1).Update(candidate);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Candidate_Not_Found()
    {
        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);

        var result = await CreateHandler().Handle(new RejectCandidateCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Candidate_Is_Already_Closed_Out()
    {
        var candidate = CreateCandidate();
        candidate.Withdraw();

        var result = await CreateHandler().Handle(new RejectCandidateCommand(candidate.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _candidates.DidNotReceive().Update(Arg.Any<Candidate>());
    }
}
