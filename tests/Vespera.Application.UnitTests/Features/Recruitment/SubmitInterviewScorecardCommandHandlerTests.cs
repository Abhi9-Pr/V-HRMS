using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class SubmitInterviewScorecardCommandHandlerTests
{
    private readonly IReadRepository<Interview> _interviewReads = Substitute.For<IReadRepository<Interview>>();
    private readonly IWriteRepository<Interview> _interviews = Substitute.For<IWriteRepository<Interview>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private SubmitInterviewScorecardCommandHandler CreateHandler() => new(_interviewReads, _interviews, _dateTimeProvider);

    private static (Interview Interview, EmployeeId InterviewerId) CreateScheduledInterview()
    {
        var interviewerId = EmployeeId.New();
        var interview = Interview.Schedule(TenantId.New(), CandidateId.New(), PipelineStageId.New(), DateTimeOffset.UtcNow.AddDays(1), [interviewerId]).Value;
        return (interview, interviewerId);
    }

    [Fact]
    public async Task Handle_Should_Add_A_Scorecard_From_A_Known_Interviewer()
    {
        var (interview, interviewerId) = CreateScheduledInterview();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(
            new SubmitInterviewScorecardCommand(interview.Id.Value, interviewerId.Value, 4, "Solid", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        interview.Scorecards.Should().ContainSingle(s => s.InterviewerId == interviewerId && s.Rating == 4);
        _interviews.Received(1).Update(interview);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Interview_Not_Found()
    {
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Interview?)null);

        var result = await CreateHandler().Handle(
            new SubmitInterviewScorecardCommand(Guid.NewGuid(), Guid.NewGuid(), 4, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Submitter_Is_Not_An_Interviewer()
    {
        var (interview, _) = CreateScheduledInterview();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(
            new SubmitInterviewScorecardCommand(interview.Id.Value, Guid.NewGuid(), 4, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
