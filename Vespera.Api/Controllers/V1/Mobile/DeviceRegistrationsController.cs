using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Http;
using Vespera.Application.Features.Mobile;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Api.Controllers.V1.Mobile;

/// <summary>Device-token CRUD backing push delivery — see docs/api-mobile-contract.md's
/// conventions. Self-service only: every action here is scoped to the caller's own devices,
/// resolved from <c>ICurrentUser</c>, never a request parameter.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mobile/devices")]
public sealed class DeviceRegistrationsController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public DeviceRegistrationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Registers this device for push, or — if the caller already registered this exact
    /// device id — refreshes its push token and reactivates it. Safe to call on every app launch;
    /// see RegisterDeviceCommandHandler's upsert-by-device-id doc comment.</summary>
    /// <response code="200">The device registration's id.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(RegisterDeviceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new RegisterDeviceCommand(request.DeviceId, request.Platform, request.PushToken, idempotencyKey);

        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new RegisterDeviceResponse(id)));
    }

    /// <summary>Stops push delivery to one of the caller's own devices — call on sign-out.</summary>
    /// <response code="204">Deactivated.</response>
    /// <response code="403">Not this device's owner.</response>
    /// <response code="404">No such device registration.</response>
    [HttpDelete("{deviceRegistrationId:guid}")]
    [Authorize]
    public async Task<IActionResult> Deactivate(Guid deviceRegistrationId, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        return (await _sender.Send(new DeactivateDeviceCommand(deviceRegistrationId, idempotencyKey), cancellationToken)).ToActionResult(this);
    }
}

public sealed record RegisterDeviceRequest(string DeviceId, DevicePlatform Platform, string PushToken);

public sealed record RegisterDeviceResponse(Guid Id);
