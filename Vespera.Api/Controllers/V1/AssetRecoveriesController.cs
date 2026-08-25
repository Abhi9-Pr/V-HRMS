using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/asset-recoveries")]
public sealed class AssetRecoveriesController : ControllerBase
{
    private readonly ISender _sender;

    public AssetRecoveriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">A page of pending recoveries.</response>
    [HttpGet]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(typeof(PagedResult<AssetRecoveryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPending([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPendingAssetRecoveriesQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="204">Courier dispatch recorded.</response>
    [HttpPost("{id:guid}/courier-dispatch")]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordCourierDispatch(
        Guid id, [FromBody] CourierDispatchRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new RecordCourierDispatchCommand(id, request.Carrier, request.TrackingReference, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Marked received.</response>
    [HttpPost("{id:guid}/received")]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordReceived(
        Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new RecordAssetReceivedCommand(id, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Damage assessment recorded.</response>
    [HttpPost("{id:guid}/damage-assessment")]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordDamageAssessment(
        Guid id, [FromBody] DamageAssessmentRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new RecordDamageAssessmentCommand(id, request.Notes, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Written off.</response>
    [HttpPost("{id:guid}/write-off")]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> WriteOff(
        Guid id, [FromBody] WriteOffRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new WriteOffAssetCommand(id, request.Amount, request.Currency, request.Reason, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Completed.</response>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Assets.Recover)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Complete(
        Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CompleteAssetRecoveryCommand(id, idempotencyKey), cancellationToken)).ToActionResult(this);
}

public sealed record CourierDispatchRequest(string Carrier, string TrackingReference);

public sealed record DamageAssessmentRequest(string Notes);

public sealed record WriteOffRequest(decimal Amount, Currency Currency, string Reason);
