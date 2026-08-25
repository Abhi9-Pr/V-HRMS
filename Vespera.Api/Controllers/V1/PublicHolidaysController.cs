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
[Route("api/v{version:apiVersion}/helpdesk/public-holidays")]
public sealed class PublicHolidaysController : ControllerBase
{
    private readonly ISender _sender;

    public PublicHolidaysController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new holiday's id.</response>
    [HttpPost]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePublicHolidayRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new CreatePublicHolidayCommand(request.Date, request.Name, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new { id }));

    [HttpGet]
    [HasPermission(Permissions.Helpdesk.ManageConfiguration)]
    public async Task<IActionResult> GetHolidays([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPublicHolidaysQuery(paging), cancellationToken)).ToActionResult(this);
}

public sealed record CreatePublicHolidayRequest(DateOnly Date, string Name);
