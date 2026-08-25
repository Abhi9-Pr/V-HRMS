using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Assets;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/offboarding-checklists")]
public sealed class OffboardingChecklistsController : ControllerBase
{
    private readonly ISender _sender;

    public OffboardingChecklistsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("employees/{employeeId:guid}")]
    [HasPermission(Permissions.Assets.Recover)]
    public async Task<IActionResult> GetForEmployee(Guid employeeId, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetOffboardingChecklistForEmployeeQuery(employeeId), cancellationToken)).ToActionResult(this);

    [HttpPost("{checklistId:guid}/items/{itemIndex:int}/complete")]
    [HasPermission(Permissions.Assets.Recover)]
    public async Task<IActionResult> CompleteItem(
        Guid checklistId, int itemIndex, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new CompleteOffboardingChecklistItemCommand(checklistId, itemIndex, idempotencyKey), cancellationToken))
            .ToActionResult(this);
}
