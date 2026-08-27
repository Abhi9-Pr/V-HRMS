using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Dashboard;

namespace Vespera.Api.Controllers.V1;

/// <summary>The landing dashboard's one aggregated read plus its layout persistence — see
/// <c>GetDashboardQueryHandler</c> for how the per-widget parallel fetch, cache, and failure
/// isolation work.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every visible widget's payload plus the caller's full layout.</response>
    [HttpGet]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    [ProducesResponseType(typeof(DashboardResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        (await _sender.Send(new GetDashboardQuery(), cancellationToken)).ToActionResult(this);

    /// <summary>The caller's full widget arrangement (order, visibility, size) — the client always
    /// sends the whole set, not a single move/hide/resize. The <c>Idempotency-Key</c> header (if
    /// present) dedups a retried save.</summary>
    /// <response code="204">Saved.</response>
    /// <response code="400">Validation failed, or a widget key isn't currently registered.</response>
    [HttpPut("layout")]
    [HasPermission(Permissions.Workspace.ViewDashboard)]
    public async Task<IActionResult> SaveLayout(SaveDashboardLayoutRequest request, CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        var command = new SaveDashboardLayoutCommand(request.Widgets, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }
}

public sealed record SaveDashboardLayoutRequest(IReadOnlyList<WidgetPreferenceInput> Widgets);
