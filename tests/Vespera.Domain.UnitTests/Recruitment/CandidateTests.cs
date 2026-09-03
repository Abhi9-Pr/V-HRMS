using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Recruitment;

public class CandidateTests
{
    [Fact]
    public void Create_Should_Fail_With_Blank_Name()
    {
        var result = Candidate.Create(
            TenantId.New(), JobRequisitionId.New(), "   ", EmailAddress.Create("a@b.com").Value, PhoneNumber.Create("+14155552671").Value);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MoveToStage_Should_Set_Stage_And_Status()
    {
        var candidate = CreateCandidate();
        var stageId = PipelineStageId.New();

        var result = candidate.MoveToStage(stageId);

        result.IsSuccess.Should().BeTrue();
        candidate.CurrentPipelineStageId.Should().Be(stageId);
        candidate.Status.Should().Be(CandidateStatus.InPipeline);
    }

    [Theory]
    [InlineData(CandidateStatus.Hired)]
    [InlineData(CandidateStatus.Rejected)]
    [InlineData(CandidateStatus.Withdrawn)]
    public void MoveToStage_Should_Fail_Once_Closed_Out(CandidateStatus terminalStatus)
    {
        var candidate = CreateCandidate();
        Transition(candidate, terminalStatus);

        var result = candidate.MoveToStage(PipelineStageId.New());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reject_Should_Fail_When_Already_Hired()
    {
        var candidate = CreateCandidate();
        candidate.MarkHired();

        var result = candidate.Reject();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Withdraw_Should_Succeed_While_Active()
    {
        var candidate = CreateCandidate();

        var result = candidate.Withdraw();

        result.IsSuccess.Should().BeTrue();
        candidate.Status.Should().Be(CandidateStatus.Withdrawn);
    }

    [Fact]
    public void Withdraw_Should_Fail_Once_Closed_Out()
    {
        var candidate = CreateCandidate();
        candidate.Withdraw();

        var result = candidate.Withdraw();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("candidate.not_active");
    }

    [Fact]
    public void MarkOffered_Should_Succeed_While_Active()
    {
        var candidate = CreateCandidate();

        var result = candidate.MarkOffered();

        result.IsSuccess.Should().BeTrue();
        candidate.Status.Should().Be(CandidateStatus.Offered);
    }

    [Fact]
    public void MarkOffered_Should_Fail_Once_Closed_Out()
    {
        var candidate = CreateCandidate();
        candidate.Reject();

        var result = candidate.MarkOffered();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("candidate.not_active");
    }

    [Fact]
    public void MarkHired_Should_Succeed_While_Active()
    {
        var candidate = CreateCandidate();

        var result = candidate.MarkHired();

        result.IsSuccess.Should().BeTrue();
        candidate.Status.Should().Be(CandidateStatus.Hired);
    }

    [Fact]
    public void Reject_Should_Succeed_While_Active()
    {
        var candidate = CreateCandidate();

        var result = candidate.Reject();

        result.IsSuccess.Should().BeTrue();
        candidate.Status.Should().Be(CandidateStatus.Rejected);
    }

    [Fact]
    public void CandidateId_New_Should_Generate_Distinct_Values()
    {
        CandidateId.New().Should().NotBe(CandidateId.New());
    }

    private static void Transition(Candidate candidate, CandidateStatus status)
    {
        switch (status)
        {
            case CandidateStatus.Hired:
                candidate.MarkHired();
                break;
            case CandidateStatus.Rejected:
                candidate.Reject();
                break;
            case CandidateStatus.Withdrawn:
                candidate.Withdraw();
                break;
        }
    }

    private static Candidate CreateCandidate() =>
        Candidate.Create(
            TenantId.New(), JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
}
