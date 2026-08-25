using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/recruitment/interviews")]
public sealed class InterviewsController : ControllerBase
{
    private readonly ISender _sender;

    public InterviewsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new interview's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(typeof(ScheduleInterviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleInterviewRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
                new ScheduleInterviewCommand(request.CandidateId, request.PipelineStageId, request.ScheduledAt, request.InterviewerIds, idempotencyKey),
                cancellationToken))
            .ToActionResult(this, id => Ok(new ScheduleInterviewResponse(id)));

    /// <response code="204">Scorecard submitted.</response>
    [HttpPost("{interviewId:guid}/scorecards")]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitScorecard(
        Guid interviewId, [FromBody] SubmitScorecardRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(
                new SubmitInterviewScorecardCommand(interviewId, request.InterviewerId, request.Rating, request.Notes, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Completed.</response>
    [HttpPost("{interviewId:guid}/complete")]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(
        Guid interviewId, [FromBody] CompleteInterviewRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new CompleteInterviewCommand(interviewId, request.Feedback, request.Rating, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Cancelled.</response>
    [HttpPost("{interviewId:guid}/cancel")]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(
        Guid interviewId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CancelInterviewCommand(interviewId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Rescheduled.</response>
    [HttpPost("{interviewId:guid}/reschedule")]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reschedule(
        Guid interviewId, [FromBody] RescheduleInterviewRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new RescheduleInterviewCommand(interviewId, request.NewScheduledAt, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="200">The candidate's interviews.</response>
    [HttpGet("by-candidate/{candidateId:guid}")]
    [HasPermission(Permissions.Recruitment.ManageInterviews)]
    [ProducesResponseType(typeof(IReadOnlyList<InterviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForCandidate(Guid candidateId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetInterviewsForCandidateQuery(candidateId), cancellationToken)).ToActionResult(this);
}

public sealed record ScheduleInterviewResponse(Guid Id);

public sealed record ScheduleInterviewRequest(Guid CandidateId, Guid PipelineStageId, DateTimeOffset ScheduledAt, IReadOnlyList<Guid> InterviewerIds);

public sealed record SubmitScorecardRequest(Guid InterviewerId, int Rating, string? Notes);

public sealed record CompleteInterviewRequest(string Feedback, int Rating);

public sealed record RescheduleInterviewRequest(DateTimeOffset NewScheduledAt);
