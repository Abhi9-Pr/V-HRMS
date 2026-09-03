using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CancelInterviewCommandHandlerTests
{
    private readonly IReadRepository<Interview> _interviewReads = Substitute.For<IReadRepository<Interview>>();
    private readonly IWriteRepository<Interview> _interviews = Substitute.For<IWriteRepository<Interview>>();

    private CancelInterviewCommandHandler CreateHandler() => new(_interviewReads, _interviews);

    private static Interview CreateScheduledInterview() =>
        Interview.Schedule(TenantId.New(), CandidateId.New(), PipelineStageId.New(), DateTimeOffset.UtcNow.AddDays(1), [EmployeeId.New()]).Value;

    [Fact]
    public async Task Handle_Should_Cancel_The_Interview()
    {
        var interview = CreateScheduledInterview();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);

        var result = await CreateHandler().Handle(new CancelInterviewCommand(interview.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        interview.Status.Should().Be(InterviewStatus.Cancelled);
        _interviews.Received(1).Update(interview);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Interview_Not_Found()
    {
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Interview?)null);

        var result = await CreateHandler().Handle(new CancelInterviewCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Interview_Is_Not_Scheduled()
    {
        var interview = CreateScheduledInterview();
        interview.Cancel();
        _interviewReads.FirstOrDefaultAsync(Arg.Any<InterviewByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(interview);

        var result = await CreateHandler().Handle(new CancelInterviewCommand(interview.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
