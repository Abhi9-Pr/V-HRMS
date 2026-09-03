using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class RescheduleInterviewCommandHandlerTests
{
    private readonly IReadRepository<Interview> _interviewReads = Substitute.For<IReadRepository<Interview>>();
    private readonly IWriteRepository<Interview> _interviews = Substitute.For<IWriteRepository<Interview>>();

    private RescheduleInterviewCommandHandler CreateHandler() => new(_interviewReads, _interviews);

    private static Interview CreateScheduledInterview() =>
        Interview.Schedule(TenantId.New(), CandidateId.New(), PipelineStageId.New(), DateTimeOffset.UtcNow.AddDays(1), [EmployeeId.New()]).Value;

    [Fact]
    public async Task Handle_Should_Reschedule_The_Interview()
    {
        var interview = CreateScheduledInterview();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);
        var newTime = DateTimeOffset.UtcNow.AddDays(5);

        var result = await CreateHandler().Handle(new RescheduleInterviewCommand(interview.Id.Value, newTime, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        interview.ScheduledAt.Should().Be(newTime);
        _interviews.Received(1).Update(interview);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Interview_Not_Found()
    {
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Interview?)null);

        var result = await CreateHandler().Handle(
            new RescheduleInterviewCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Interview_Is_Not_Scheduled()
    {
        var interview = CreateScheduledInterview();
        interview.Cancel();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);

        var result = await CreateHandler().Handle(
            new RescheduleInterviewCommand(interview.Id.Value, DateTimeOffset.UtcNow.AddDays(1), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
