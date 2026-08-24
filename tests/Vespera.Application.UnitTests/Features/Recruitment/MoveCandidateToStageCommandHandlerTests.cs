using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Application.Features.Recruitment.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class MoveCandidateToStageCommandHandlerTests
{
    private readonly IReadRepository<Candidate> _candidateReads = Substitute.For<IReadRepository<Candidate>>();
    private readonly IWriteRepository<Candidate> _candidates = Substitute.For<IWriteRepository<Candidate>>();
    private readonly IReadRepository<JobRequisition> _requisitions = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IReadRepository<Interview> _interviews = Substitute.For<IReadRepository<Interview>>();
    private readonly TenantId _tenantId = TenantId.New();

    private (Candidate Candidate, JobRequisition Requisition, PipelineStageId ScreeningStageId, PipelineStageId OfferStageId) CreateCandidateInPipeline()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Screening", DateTimeOffset.UtcNow, "system");
        requisition.AddStage("Offer", DateTimeOffset.UtcNow, "system");

        var candidate = Candidate.Create(
            _tenantId, requisition.Id, "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;

        _candidateReads.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _interviews.ListAsync(Arg.Any<InterviewsByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        return (candidate, requisition, requisition.Stages.Single(s => s.Name == "Screening").Id, requisition.Stages.Single(s => s.Name == "Offer").Id);
    }

    private MoveCandidateToStageCommandHandler CreateHandler() => new(
        _candidateReads, _candidates, _requisitions, _interviews,
        new StageTransitionEvaluator([new RequiresCompletedInterviewBeforeOfferStageRule()]));

    [Fact]
    public async Task Handle_Should_Allow_Moving_To_A_Non_Offer_Stage_With_No_Interviews()
    {
        var (candidate, _, screeningStageId, _) = CreateCandidateInPipeline();

        var result = await CreateHandler().Handle(
            new MoveCandidateToStageCommand(candidate.Id.Value, screeningStageId.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        candidate.CurrentPipelineStageId.Should().Be(screeningStageId);
    }

    [Fact]
    public async Task Handle_Should_Block_Moving_To_The_Offer_Stage_Before_Any_Completed_Interview()
    {
        var (candidate, _, _, offerStageId) = CreateCandidateInPipeline();

        var result = await CreateHandler().Handle(
            new MoveCandidateToStageCommand(candidate.Id.Value, offerStageId.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        candidate.CurrentPipelineStageId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Allow_Moving_To_The_Offer_Stage_Once_An_Interview_Is_Completed()
    {
        var (candidate, _, _, offerStageId) = CreateCandidateInPipeline();
        var interview = Interview.Schedule(
            _tenantId, candidate.Id, offerStageId, DateTimeOffset.UtcNow.AddDays(1), [EmployeeId.New()]).Value;
        interview.Complete("Great candidate", 5);
        _interviews.ListAsync(Arg.Any<InterviewsByCandidateSpecification>(), Arg.Any<CancellationToken>()).Returns([interview]);

        var result = await CreateHandler().Handle(
            new MoveCandidateToStageCommand(candidate.Id.Value, offerStageId.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        candidate.CurrentPipelineStageId.Should().Be(offerStageId);
    }
}
