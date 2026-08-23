using System.Net;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptions<WebPunchOptions> _webPunchOptions;

    public AttendanceController(ISender sender, IAuthorizationService authorizationService, IOptions<WebPunchOptions> webPunchOptions)
    {
        _sender = sender;
        _authorizationService = authorizationService;
        _webPunchOptions = webPunchOptions;
    }

    /// <summary>Records a web punch. Authorized the same way a resource-scoped employee read is
    /// (<see cref="SubordinateOrSelfRequirement"/>): an employee may always punch for themselves;
    /// punching on someone else's behalf (a manager/HR correction) additionally requires
    /// <see cref="Permissions.Attendance"/>'s <c>ManageTeam</c> override. There's no separate
    /// "self-punch" permission constant — punching for yourself isn't a permission grant, it's
    /// just being you.</summary>
    /// <response code="204">Punch recorded.</response>
    /// <response code="400">Validation failed, or the punch is outside the assigned geofence.</response>
    /// <response code="403">Not this employee, not their manager, and no ManageTeam override; or the caller's IP isn't allowlisted.</response>
    [HttpPost("punch")]
    [Authorize]
    public async Task<IActionResult> Punch(RecordWebPunchRequest request, CancellationToken cancellationToken)
    {
        var resourceAuthorization = await _authorizationService.AuthorizeAsync(
            User, new EmployeeId(request.EmployeeId), new SubordinateOrSelfRequirement(Permissions.Attendance.ManageTeam));
        if (!resourceAuthorization.Succeeded)
        {
            return Forbid();
        }

        var options = _webPunchOptions.Value;
        if (options.AllowlistEnabled)
        {
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            var allowed = remoteIp is not null && options.Cidrs.Any(cidr =>
                IPNetwork.TryParse(cidr, out var network) && network.Contains(remoteIp));
            if (!allowed)
            {
                return Result.Failure(Error.Forbidden("attendance.punch.ip_not_allowed", "This network is not allowed to record punches."))
                    .ToActionResult(this);
            }
        }

        var command = new RecordWebPunchCommand(request.EmployeeId, request.PunchType, request.Latitude, request.Longitude);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <summary>Clears a flagged punch (mock-provider, impossible-travel, low-accuracy) after a
    /// manager's review. A plain state flip, not a second approval workflow — reviewing someone
    /// else's punch is inherently a manager action, so this always requires
    /// <see cref="Permissions.Attendance"/>'s <c>ManageTeam</c>, unlike punching itself which an
    /// employee may always do for themselves.</summary>
    /// <response code="204">Flag cleared.</response>
    /// <response code="404">No such punch on that employee's attendance day for that date.</response>
    [HttpPost("punches/clear-flag")]
    [HasPermission(Permissions.Attendance.ManageTeam)]
    public async Task<IActionResult> ClearPunchFlag(ClearPunchFlagRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ClearPunchFlagCommand(request.EmployeeId, request.Date, request.PunchId), cancellationToken)).ToActionResult(this);

    /// <summary>Reads back one employee's attendance day, including whatever
    /// <c>AttendanceDayCalculator</c> last computed for it.</summary>
    /// <response code="200">The attendance day.</response>
    /// <response code="403">Not this employee, not their manager, and no ManageTeam override.</response>
    /// <response code="404">No attendance day exists for that employee and date.</response>
    [HttpGet("days")]
    [Authorize]
    [ProducesResponseType(typeof(AttendanceDayDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDay([FromQuery] Guid employeeId, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        var resourceAuthorization = await _authorizationService.AuthorizeAsync(
            User, new EmployeeId(employeeId), new SubordinateOrSelfRequirement(Permissions.Attendance.ManageTeam));
        if (!resourceAuthorization.Succeeded)
        {
            return Forbid();
        }

        return (await _sender.Send(new GetAttendanceDayQuery(employeeId, date), cancellationToken)).ToActionResult(this);
    }

    /// <summary>On-demand recompute of an employee's attendance-day state for a date range —
    /// the same handler the nightly sweep (<c>AttendanceDayComputationHostedService</c>) calls for
    /// "yesterday," available here for an HR-triggered correction over any range.</summary>
    /// <response code="204">Recomputed.</response>
    [HttpPost("recompute")]
    [HasPermission(Permissions.Attendance.ManageTeam)]
    public async Task<IActionResult> Recompute(RecomputeAttendanceDayRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new RecomputeAttendanceDayCommand(request.EmployeeId, request.RangeStart, request.RangeEnd), cancellationToken))
            .ToActionResult(this);
}

public sealed record RecordWebPunchRequest(Guid EmployeeId, string PunchType, double? Latitude, double? Longitude);

public sealed record ClearPunchFlagRequest(Guid EmployeeId, DateOnly Date, Guid PunchId);

public sealed record RecomputeAttendanceDayRequest(Guid EmployeeId, DateOnly RangeStart, DateOnly RangeEnd);
