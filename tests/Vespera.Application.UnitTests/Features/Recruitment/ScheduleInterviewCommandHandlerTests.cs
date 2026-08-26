using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class ScheduleInterviewCommandHandlerTests
{
    private readonly IWriteRepository<Interview> _interviews = Substitute.For<IWriteRepository<Interview>>();
    private readonly IReadRepository<Candidate> _candidates = Substitute.For<IReadRepository<Candidate>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();

    public ScheduleInterviewCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private Candidate CreateCandidate()
    {
        var candidate = Candidate.Create(
            _tenantId, JobRequisitionId.New(), "Jordan Lee", EmailAddress.Create("jordan.lee@example.com").Value,
            PhoneNumber.Create("+14155552671").Value).Value;
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(candidate);
        return candidate;
    }

    private ScheduleInterviewCommandHandler CreateHandler() => new(_interviews, _candidates, _tenantContext, _notificationDispatcher);

    [Fact]
    public async Task Handle_Should_Create_The_Interview_And_Notify_Every_Interviewer_And_The_Candidate()
    {
        var candidate = CreateCandidate();
        var interviewerA = EmployeeId.New();
        var interviewerB = EmployeeId.New();

        var result = await CreateHandler().Handle(
            new ScheduleInterviewCommand(
                candidate.Id.Value, PipelineStageId.New().Value, DateTimeOffset.UtcNow.AddDays(2),
                [interviewerA.Value, interviewerB.Value], null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _interviews.Received(1).AddAsync(
            Arg.Is<Interview>(i => i.InterviewerIds.Contains(interviewerA) && i.InterviewerIds.Contains(interviewerB)),
            Arg.Any<CancellationToken>());

        // Two interviewers + the candidate = three notifications, each carrying the ICS text.
        await _notificationDispatcher.Received(3).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.Metadata.ContainsKey("icsContent")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Candidate_Is_Not_Found()
    {
        _candidates.FirstOrDefaultAsync(Arg.Any<CandidateByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Candidate?)null);

        var result = await CreateHandler().Handle(
            new ScheduleInterviewCommand(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(1), [Guid.NewGuid()], null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
