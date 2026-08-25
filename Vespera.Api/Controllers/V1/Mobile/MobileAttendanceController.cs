using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1.Mobile;

/// <summary>The first real <c>api/v1/mobile/*</c> route in the codebase — see
/// docs/api-mobile-contract.md for the conventions this follows (Idempotency-Key replay via the
/// global <c>IdempotencyMiddleware</c>, dedup via <c>IIdempotentRequest</c>).</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/attendance")]
public sealed class MobileAttendanceController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;
    private readonly IAuthorizationService _authorizationService;
    private readonly IOptions<MobilePunchOptions> _mobilePunchOptions;

    public MobileAttendanceController(ISender sender, IAuthorizationService authorizationService, IOptions<MobilePunchOptions> mobilePunchOptions)
    {
        _sender = sender;
        _authorizationService = authorizationService;
        _mobilePunchOptions = mobilePunchOptions;
    }

    /// <summary>Same self-or-manager authorization as the web punch endpoint — see
    /// <see cref="AttendanceController.Punch"/>'s doc comment. Every rejection (outside the
    /// geofence, a mock-provider location with the reject policy enabled) returns a specific
    /// <c>Error.Code</c> the app can read off the ProblemDetails body; anything else (impossible
    /// travel, low accuracy) is a successful punch flagged for a manager's review, never a
    /// rejection.</summary>
    /// <response code="204">Punch recorded (possibly flagged for review).</response>
    /// <response code="400">Validation failed, outside the geofence, or a rejected mock-provider location.</response>
    /// <response code="403">Not this employee, not their manager, and no ManageTeam override.</response>
    [HttpPost("punch")]
    [Authorize]
    public async Task<IActionResult> Punch(RecordMobilePunchRequest request, CancellationToken cancellationToken)
    {
        var resourceAuthorization = await _authorizationService.AuthorizeAsync(
            User, new EmployeeId(request.EmployeeId), new SubordinateOrSelfRequirement(Permissions.Attendance.ManageTeam));
        if (!resourceAuthorization.Succeeded)
        {
            return Forbid();
        }

        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var options = _mobilePunchOptions.Value;
        var command = new RecordMobilePunchCommand(
            request.EmployeeId,
            request.PunchType,
            request.Latitude,
            request.Longitude,
            request.Accuracy,
            request.IsFromMockProvider,
            request.DeviceId,
            options.RejectMockProvider,
            options.MaxPlausibleSpeedKmh,
            options.MinAcceptableAccuracyMetres,
            idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <summary>What's changed in the caller's own attendance since <paramref name="since"/> — see
    /// docs/api-mobile-contract.md's delta-sync convention. Self-service only: always scoped to
    /// whichever employee the caller's own account is linked to, never a request parameter.</summary>
    /// <response code="200">Upserts and tombstones since <paramref name="since"/>.</response>
    [HttpGet("sync")]
    [Authorize]
    [ProducesResponseType(typeof(DeltaSyncResult<AttendanceDaySummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(
        [FromQuery] DateTimeOffset since, [FromQuery] string? cursor, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyAttendanceDeltaSyncQuery(since, cursor, pageSize), cancellationToken)).ToActionResult(this);
}

public sealed record RecordMobilePunchRequest(
    Guid EmployeeId,
    string PunchType,
    double? Latitude,
    double? Longitude,
    double Accuracy,
    bool IsFromMockProvider,
    string DeviceId);
