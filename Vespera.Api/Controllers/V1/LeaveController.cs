using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Leave;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave")]
public sealed class LeaveController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public LeaveController(ISender sender)
    {
        _sender = sender;
    }

    private string? IdempotencyKey => Request.Headers[IdempotencyKeyHeader].FirstOrDefault();

    /// <response code="200">The new request's id, the day count charged, and any loss-of-pay days.</response>
    /// <response code="400">Validation failed, or insufficient balance without acknowledgement.</response>
    /// <response code="409">Overlaps an existing request, or falls within a blackout period.</response>
    [HttpPost("requests")]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(SubmitLeaveRequestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitLeaveRequest(SubmitLeaveRequestCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="200">The caller's own requests, most recent first.</response>
    [HttpGet("requests/mine")]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLeaveRequests(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyLeaveRequestsQuery(), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see ApproveLeaveRequestCommand's IIdempotentRequest. Matters here specifically
    /// because this is the action a push-notification deep link opens: a manager approving on
    /// flaky mobile connectivity can safely retry without double-processing.</summary>
    /// <response code="204">Approved (or advanced to the next tier).</response>
    /// <response code="403">Not the authorized approver for the current tier.</response>
    /// <response code="409">The chain is no longer in progress.</response>
    [HttpPost("requests/{requestId:guid}/approve")]
    [HasPermission(Permissions.Leave.Approve)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid requestId, CancellationToken cancellationToken) =>
        (await _sender.Send(new ApproveLeaveRequestCommand(requestId, Comment: null, IdempotencyKey), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see RejectLeaveRequestCommand's IIdempotentRequest.</summary>
    /// <response code="204">Rejected.</response>
    /// <response code="403">Not the authorized approver for the current tier.</response>
    [HttpPost("requests/{requestId:guid}/reject")]
    [HasPermission(Permissions.Leave.Approve)]
    public async Task<IActionResult> RejectLeaveRequest(Guid requestId, RejectLeaveRequestBody body, CancellationToken cancellationToken) =>
        (await _sender.Send(new RejectLeaveRequestCommand(requestId, body.Reason, IdempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Withdrawn.</response>
    /// <response code="403">Not the requester.</response>
    /// <response code="409">Not pending.</response>
    [HttpPost("requests/{requestId:guid}/withdraw")]
    [HasPermission(Permissions.Leave.Cancel)]
    public async Task<IActionResult> WithdrawLeaveRequest(Guid requestId, CancellationToken cancellationToken) =>
        (await _sender.Send(new WithdrawLeaveRequestCommand(requestId, IdempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Cancelled, and the ledger debit reversed.</response>
    /// <response code="403">Not the requester.</response>
    /// <response code="409">Not approved, or the leave has already started.</response>
    [HttpPost("requests/{requestId:guid}/cancel")]
    [HasPermission(Permissions.Leave.Cancel)]
    public async Task<IActionResult> CancelLeaveRequest(Guid requestId, CancelLeaveRequestBody body, CancellationToken cancellationToken) =>
        (await _sender.Send(new CancelApprovedLeaveRequestCommand(requestId, body.Reason, IdempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">The caller's current balance for the given leave type, as a projection over its ledger.</response>
    [HttpGet("balance")]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(LeaveBalanceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaveBalance([FromQuery] Guid leaveTypeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetLeaveBalanceQuery(leaveTypeId), cancellationToken)).ToActionResult(this);

    /// <response code="200">Every request the caller is currently the authorized approver for.</response>
    [HttpGet("approval-inbox")]
    [HasPermission(Permissions.Leave.ReadTeam)]
    [ProducesResponseType(typeof(IReadOnlyList<ApprovalInboxItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovalInbox(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetApprovalInboxQuery(), cancellationToken)).ToActionResult(this);

    /// <response code="200">Who is out per day in the range, with a conflict warning when too many overlap.</response>
    [HttpGet("team-calendar")]
    [HasPermission(Permissions.Leave.ReadTeam)]
    [ProducesResponseType(typeof(IReadOnlyList<TeamLeaveCalendarDayDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamLeaveCalendar(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? departmentId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetTeamLeaveCalendarQuery(from, to, departmentId), cancellationToken)).ToActionResult(this);

    /// <response code="204">Encashed.</response>
    /// <response code="409">Insufficient balance, or the leave type is not encashable.</response>
    [HttpPost("encashments")]
    [HasPermission(Permissions.Leave.Encash)]
    public async Task<IActionResult> EncashLeave(EncashLeaveCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <summary>What's changed in the caller's own leave requests since <paramref name="since"/> —
    /// see docs/api-mobile-contract.md's delta-sync convention. Self-service only: always scoped
    /// to whichever employee the caller's own account is linked to, never a request parameter.</summary>
    /// <response code="200">Upserts since <paramref name="since"/> (leave requests are never
    /// tombstoned — see <see cref="GetMyLeaveDeltaSyncQuery"/>'s doc comment).</response>
    [HttpGet("sync")]
    [HasPermission(Permissions.Leave.Request)]
    [ProducesResponseType(typeof(DeltaSyncResult<LeaveRequestSyncDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(
        [FromQuery] DateTimeOffset since, [FromQuery] string? cursor, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyLeaveDeltaSyncQuery(since, cursor, pageSize), cancellationToken)).ToActionResult(this);
}

public sealed record RejectLeaveRequestBody(string Reason);

public sealed record CancelLeaveRequestBody(string Reason);
