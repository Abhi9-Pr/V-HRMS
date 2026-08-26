using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly ISender _sender;

    public AssetsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new asset's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Assets.Write)]
    [ProducesResponseType(typeof(CreateAssetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsset(
        [FromBody] CreateAssetRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new CreateAssetCommand(
                request.AssetTag, request.Category, request.PurchaseCost, request.PurchaseCostCurrency, request.PurchaseDate,
                request.SerialNumber, request.MacAddress, request.WarrantyExpiryDate, idempotencyKey),
            cancellationToken))
            .ToActionResult(this, id => Ok(new CreateAssetResponse(id)));

    /// <response code="204">Depreciation configured.</response>
    [HttpPost("{id:guid}/depreciation")]
    [HasPermission(Permissions.Assets.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfigureDepreciation(
        Guid id, [FromBody] ConfigureDepreciationRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(
            new ConfigureAssetDepreciationCommand(id, request.Method, request.UsefulLifeMonths, request.SalvageValue, request.SalvageValueCurrency, idempotencyKey),
            cancellationToken))
            .ToActionResult(this);

    /// <response code="200">The new assignment's id.</response>
    [HttpPost("{id:guid}/assign")]
    [HasPermission(Permissions.Assets.Assign)]
    [ProducesResponseType(typeof(AssignAssetResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignAsset(
        Guid id, [FromBody] AssignAssetRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new AssignAssetCommand(id, request.EmployeeId, idempotencyKey), cancellationToken))
            .ToActionResult(this, assignmentId => Ok(new AssignAssetResponse(assignmentId)));

    /// <response code="200">The uploaded signature's storage reference.</response>
    [HttpPost("assignments/{assignmentId:guid}/signature")]
    [HasPermission(Permissions.Assets.Assign)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadSignatureResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadHandoverSignature(
        Guid assignmentId, IFormFile file, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var command = new UploadHandoverSignatureCommand(assignmentId, buffer.ToArray(), file.FileName, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, reference => Ok(new UploadSignatureResponse(reference)));
    }

    /// <response code="204">Condition report recorded.</response>
    [HttpPost("assignments/{assignmentId:guid}/condition-reports")]
    [HasPermission(Permissions.Assets.Assign)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordCondition(
        Guid assignmentId, [FromBody] RecordConditionRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new RecordAssetConditionCommand(assignmentId, request.Rating, request.Notes, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Returned.</response>
    [HttpPost("assignments/{assignmentId:guid}/return")]
    [HasPermission(Permissions.Assets.Assign)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReturnAsset(
        Guid assignmentId, [FromBody] ReturnAssetRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new ReturnAssetCommand(assignmentId, request.Condition, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="204">Marked under repair.</response>
    [HttpPost("{id:guid}/under-repair")]
    [HasPermission(Permissions.Assets.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkUnderRepair(
        Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new MarkAssetUnderRepairCommand(id, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Retired.</response>
    [HttpPost("{id:guid}/retire")]
    [HasPermission(Permissions.Assets.Write)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Retire(
        Guid id, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new RetireAssetCommand(id, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">A page of assets.</response>
    [HttpGet]
    [HasPermission(Permissions.Assets.Read)]
    [ProducesResponseType(typeof(PagedResult<AssetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssets([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAssetsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The asset detail.</response>
    /// <response code="404">No such asset.</response>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Assets.Read)]
    [ProducesResponseType(typeof(AssetDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssetById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetAssetByIdQuery(id), cancellationToken)).ToActionResult(this);
}

public sealed record CreateAssetResponse(Guid Id);

public sealed record AssignAssetResponse(Guid Id);

public sealed record UploadSignatureResponse(string Reference);

public sealed record CreateAssetRequest(
    string AssetTag, string Category, decimal PurchaseCost, Currency PurchaseCostCurrency, DateOnly PurchaseDate,
    string? SerialNumber, string? MacAddress, DateOnly? WarrantyExpiryDate);

public sealed record ConfigureDepreciationRequest(DepreciationMethod Method, int UsefulLifeMonths, decimal SalvageValue, Currency SalvageValueCurrency);

public sealed record AssignAssetRequest(Guid EmployeeId);

public sealed record RecordConditionRequest(AssetConditionRating Rating, string? Notes);

public sealed record ReturnAssetRequest(string Condition);
