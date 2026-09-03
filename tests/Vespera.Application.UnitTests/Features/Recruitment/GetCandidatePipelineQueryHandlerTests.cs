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

public class GetCandidatePipelineQueryHandlerTests
{
    private readonly IReadRepository<JobRequisition> _requisitions = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IReadRepository<Candidate> _candidates = Substitute.For<IReadRepository<Candidate>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetCandidatePipelineQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetCandidatePipelineQueryHandler CreateHandler() => new(_requisitions, _candidates, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Stages_And_Candidates_For_The_Requisition()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Screening", DateTimeOffset.UtcNow, "system");
        var candidate = Candidate.Create(
            _tenantId, requisition.Id, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;

        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _candidates.ListAsync(Arg.Any<CandidatesByRequisitionSpecification>(), Arg.Any<CancellationToken>()).Returns([candidate]);

        var result = await CreateHandler().Handle(new GetCandidatePipelineQuery(requisition.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stages.Should().ContainSingle(s => s.Name == "Screening");
        result.Value.Candidates.Should().ContainSingle(c => c.FullName == "Jordan Lee");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Requisition_Not_Found()
    {
        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((JobRequisition?)null);

        var result = await CreateHandler().Handle(new GetCandidatePipelineQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
