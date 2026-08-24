using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{id:guid}/exit")]
    [HasPermission(Permissions.Employees.Write)]
    public async Task<IActionResult> Exit(
        Guid id, [FromBody] ExitEmployeeRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new ExitEmployeeCommand(id, request.ExitDate, request.Reason, idempotencyKey), cancellationToken))
            .ToActionResult(this);
}

public sealed record ExitEmployeeRequest(DateOnly ExitDate, EmployeeExitReason Reason);
