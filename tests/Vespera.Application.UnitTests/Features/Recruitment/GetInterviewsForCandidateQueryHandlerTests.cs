using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetInterviewsForCandidateQueryHandlerTests
{
    private readonly IReadRepository<Interview> _interviews = Substitute.For<IReadRepository<Interview>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetInterviewsForCandidateQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetInterviewsForCandidateQueryHandler CreateHandler() => new(_interviews, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Interviews_For_The_Candidate()
    {
        var candidateId = CandidateId.New();
        var interview = Interview.Schedule(
            _tenantId, candidateId, PipelineStageId.New(), DateTimeOffset.UtcNow.AddDays(1), [EmployeeId.New()]).Value;
        _interviews.ListAsync(Arg.Any<InterviewsByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([interview]);

        var result = await CreateHandler().Handle(new GetInterviewsForCandidateQuery(candidateId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(i => i.Id == interview.Id.Value && i.CandidateId == candidateId.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_Interviews()
    {
        _interviews.ListAsync(Arg.Any<InterviewsByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetInterviewsForCandidateQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
