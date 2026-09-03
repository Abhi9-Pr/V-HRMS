using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Domain.UnitTests.Recruitment;

public class InterviewTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly CandidateId CandidateId = CandidateId.New();
    private static readonly PipelineStageId PipelineStageId = PipelineStageId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Interview CreateInterview(params EmployeeId[] interviewers) =>
        Interview.Schedule(TenantId, CandidateId, PipelineStageId, Now, interviewers.Length == 0 ? [EmployeeId.New()] : interviewers).Value;

    [Fact]
    public void Schedule_Should_Fail_When_No_Interviewers_Are_Given()
    {
        var result = Interview.Schedule(TenantId, CandidateId, PipelineStageId, Now, []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.no_interviewers");
    }

    [Fact]
    public void Schedule_Should_Succeed_And_Start_Scheduled()
    {
        var interview = CreateInterview();

        interview.Status.Should().Be(InterviewStatus.Scheduled);
    }

    [Fact]
    public void Complete_Should_Succeed_With_A_Valid_Rating()
    {
        var interview = CreateInterview();

        var result = interview.Complete("Strong candidate", 5);

        result.IsSuccess.Should().BeTrue();
        interview.Status.Should().Be(InterviewStatus.Completed);
        interview.Feedback.Should().Be("Strong candidate");
        interview.Rating.Should().Be(5);
    }

    [Fact]
    public void Complete_Should_Fail_When_Not_Scheduled()
    {
        var interview = CreateInterview();
        interview.Complete("Good", 4);

        var result = interview.Complete("Good again", 4);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.not_scheduled");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Complete_Should_Fail_When_Rating_Is_Out_Of_Range(int rating)
    {
        var interview = CreateInterview();

        var result = interview.Complete("Feedback", rating);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.invalid_rating");
    }

    [Fact]
    public void Cancel_Should_Succeed_While_Scheduled()
    {
        var interview = CreateInterview();

        var result = interview.Cancel();

        result.IsSuccess.Should().BeTrue();
        interview.Status.Should().Be(InterviewStatus.Cancelled);
    }

    [Fact]
    public void Cancel_Should_Fail_When_Not_Scheduled()
    {
        var interview = CreateInterview();
        interview.Cancel();

        var result = interview.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.not_scheduled");
    }

    [Fact]
    public void Reschedule_Should_Succeed_While_Scheduled()
    {
        var interview = CreateInterview();
        var newTime = Now.AddDays(1);

        var result = interview.Reschedule(newTime);

        result.IsSuccess.Should().BeTrue();
        interview.ScheduledAt.Should().Be(newTime);
    }

    [Fact]
    public void Reschedule_Should_Fail_When_Not_Scheduled()
    {
        var interview = CreateInterview();
        interview.Cancel();

        var result = interview.Reschedule(Now.AddDays(1));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.not_scheduled");
    }

    [Fact]
    public void SubmitScorecard_Should_Fail_When_Not_An_Interviewer()
    {
        var interviewer = EmployeeId.New();
        var interview = CreateInterview(interviewer);

        var result = interview.SubmitScorecard(EmployeeId.New(), 4, "Notes", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.not_an_interviewer");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void SubmitScorecard_Should_Fail_When_Rating_Is_Out_Of_Range(int rating)
    {
        var interviewer = EmployeeId.New();
        var interview = CreateInterview(interviewer);

        var result = interview.SubmitScorecard(interviewer, rating, "Notes", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.invalid_rating");
    }

    [Fact]
    public void SubmitScorecard_Should_Fail_When_Already_Submitted()
    {
        var interviewer = EmployeeId.New();
        var interview = CreateInterview(interviewer);
        interview.SubmitScorecard(interviewer, 4, "Notes", Now);

        var result = interview.SubmitScorecard(interviewer, 5, "More notes", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("interview.scorecard_already_submitted");
    }

    [Fact]
    public void SubmitScorecard_Should_Succeed_And_Record_The_Scorecard()
    {
        var interviewer = EmployeeId.New();
        var interview = CreateInterview(interviewer);

        var result = interview.SubmitScorecard(interviewer, 5, "Great fit", Now);

        result.IsSuccess.Should().BeTrue();
        interview.Scorecards.Should().ContainSingle(s => s.InterviewerId == interviewer && s.Rating == 5);
    }

    [Fact]
    public void InterviewId_New_Should_Generate_Distinct_Values()
    {
        InterviewId.New().Should().NotBe(InterviewId.New());
    }
}
