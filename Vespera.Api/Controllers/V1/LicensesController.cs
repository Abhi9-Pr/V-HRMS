using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Assets;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/licenses")]
public sealed class LicensesController : ControllerBase
{
    private readonly ISender _sender;

    public LicensesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new license's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Licenses.Manage)]
    [ProducesResponseType(typeof(CreateLicenseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateLicense(
        [FromBody] CreateLicenseRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CreateSoftwareLicenseCommand(request.ProductName, request.SeatCount, request.ExpiresAt, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new CreateLicenseResponse(id)));

    /// <response code="200">A page of licenses.</response>
    [HttpGet]
    [HasPermission(Permissions.Licenses.Read)]
    [ProducesResponseType(typeof(PagedResult<SoftwareLicenseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicenses([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetSoftwareLicensesQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The new allocation's id.</response>
    [HttpPost("{id:guid}/allocations")]
    [HasPermission(Permissions.Licenses.Manage)]
    [ProducesResponseType(typeof(AllocateSeatResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AllocateSeat(
        Guid id, [FromBody] AllocateSeatRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new AllocateLicenseSeatCommand(id, request.EmployeeId, idempotencyKey), cancellationToken))
            .ToActionResult(this, allocationId => Ok(new AllocateSeatResponse(allocationId)));

    /// <response code="204">Released.</response>
    [HttpPost("allocations/{allocationId:guid}/release")]
    [HasPermission(Permissions.Licenses.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReleaseSeat(
        Guid allocationId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new ReleaseLicenseSeatCommand(allocationId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="200">The unused-seat report rows.</response>
    [HttpGet("unused-seats-report")]
    [HasPermission(Permissions.Licenses.Read)]
    [ProducesResponseType(typeof(IReadOnlyList<UnusedSeatsReportRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnusedSeatsReport(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetUnusedSeatsReportQuery(), cancellationToken)).ToActionResult(this);
}

public sealed record CreateLicenseResponse(Guid Id);

public sealed record AllocateSeatResponse(Guid Id);

public sealed record CreateLicenseRequest(string ProductName, int SeatCount, DateOnly? ExpiresAt);

public sealed record AllocateSeatRequest(Guid EmployeeId);
