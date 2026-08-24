using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/helpdesk/sla-policies")]
public sealed class SlaPoliciesController : ControllerBase
{
    private readonly ISender _sender;

    public SlaPoliciesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new policy's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSlaPolicyRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(
            new CreateSlaPolicyCommand(
                request.Name, request.ResponseTimeHours, request.ResolutionTimeHours, request.BusinessHoursStart, request.BusinessHoursEnd,
                idempotencyKey),
            cancellationToken))
            .ToActionResult(this, id => Ok(new { id }));

    [HttpGet]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> GetPolicies([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetSlaPoliciesQuery(paging), cancellationToken)).ToActionResult(this);
}

public sealed record CreateSlaPolicyRequest(
    string Name, int ResponseTimeHours, int ResolutionTimeHours, TimeOnly BusinessHoursStart, TimeOnly BusinessHoursEnd);
