using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Domain.UnitTests.Recruitment;

public class InterviewScorecardTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubmitScorecard_Should_Succeed_For_An_Interviewer()
    {
        var interviewerId = EmployeeId.New();
        var interview = CreateInterview(interviewerId);

        var result = interview.SubmitScorecard(interviewerId, 4, "Strong communication", Now);

        result.IsSuccess.Should().BeTrue();
        interview.Scorecards.Should().ContainSingle(s => s.InterviewerId == interviewerId && s.Rating == 4);
    }

    [Fact]
    public void SubmitScorecard_Should_Fail_For_A_Non_Interviewer()
    {
        var interview = CreateInterview(EmployeeId.New());

        var result = interview.SubmitScorecard(EmployeeId.New(), 4, null, Now);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void SubmitScorecard_Should_Fail_With_Invalid_Rating(int rating)
    {
        var interviewerId = EmployeeId.New();
        var interview = CreateInterview(interviewerId);

        var result = interview.SubmitScorecard(interviewerId, rating, null, Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubmitScorecard_Should_Fail_On_Duplicate_Submission()
    {
        var interviewerId = EmployeeId.New();
        var interview = CreateInterview(interviewerId);
        interview.SubmitScorecard(interviewerId, 4, null, Now);

        var result = interview.SubmitScorecard(interviewerId, 5, null, Now);

        result.IsFailure.Should().BeTrue();
    }

    private static Interview CreateInterview(EmployeeId interviewerId) =>
        Interview.Schedule(TenantId.New(), CandidateId.New(), PipelineStageId.New(), Now.AddDays(3), [interviewerId]).Value;
}
