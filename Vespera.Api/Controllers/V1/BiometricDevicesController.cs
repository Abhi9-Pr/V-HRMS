using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/biometric-devices")]
public sealed class BiometricDevicesController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public BiometricDevicesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A page of registered biometric devices.</response>
    [HttpGet]
    [HasPermission(Permissions.BiometricDevices.Manage)]
    [ProducesResponseType(typeof(PagedResult<BiometricDeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetBiometricDevicesQuery(paging), cancellationToken)).ToActionResult(this);

    /// <summary>The <c>Idempotency-Key</c> header (if present) is what actually dedups this
    /// command — see RegisterBiometricDeviceCommand's IIdempotentRequest.</summary>
    /// <response code="200">The new device's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [HasPermission(Permissions.BiometricDevices.Manage)]
    [ProducesResponseType(typeof(RegisterBiometricDeviceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterBiometricDeviceRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new RegisterBiometricDeviceCommand(
            request.LocationId, request.VendorType, request.Host, request.Port, request.ApiKeyConfigurationKey, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new RegisterBiometricDeviceResponse(id)));
    }

    /// <response code="200">A page of quarantined (unmatched device-user-id) punches awaiting resolution.</response>
    [HttpGet("quarantined-punches")]
    [HasPermission(Permissions.BiometricDevices.Manage)]
    [ProducesResponseType(typeof(PagedResult<QuarantinedBiometricPunchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListQuarantinedPunches([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetQuarantinedPunchesQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="204">Resolved — the quarantined punch is now a real attendance punch for the given employee.</response>
    /// <response code="404">No such quarantined punch.</response>
    [HttpPost("quarantined-punches/{id:guid}/resolve")]
    [HasPermission(Permissions.BiometricDevices.Manage)]
    public async Task<IActionResult> ResolveQuarantinedPunch(Guid id, ResolveQuarantinedPunchRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ResolveQuarantinedPunchCommand(id, request.EmployeeId), cancellationToken)).ToActionResult(this);
}

public sealed record RegisterBiometricDeviceRequest(Guid LocationId, string VendorType, string Host, int Port, string? ApiKeyConfigurationKey);

public sealed record RegisterBiometricDeviceResponse(Guid Id);

public sealed record ResolveQuarantinedPunchRequest(Guid EmployeeId);
