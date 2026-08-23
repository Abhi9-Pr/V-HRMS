using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1;

/// <summary>Employee self-service: every action here resolves to the caller's own declaration
/// (see <c>AddInvestmentDeclarationLineCommandHandler</c> et al. resolving EmployeeId from the
/// authenticated user) — not walled by Finance.Admin, unlike <c>InvestmentDeclarationReviewController</c>.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/investment-declarations")]
public sealed class InvestmentDeclarationsController : ControllerBase
{
    private readonly ISender _sender;

    public InvestmentDeclarationsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The declaration's id (created on first line if none existed yet).</response>
    [HttpPost("lines")]
    [HasPermission(Permissions.Payroll.SelfService)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddLine(AddInvestmentDeclarationLineCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <response code="204">Submitted.</response>
    [HttpPost("submit")]
    [HasPermission(Permissions.Payroll.SelfService)]
    public async Task<IActionResult> Submit([FromQuery] string financialYear, CancellationToken cancellationToken) =>
        (await _sender.Send(new SubmitInvestmentDeclarationCommand(financialYear), cancellationToken)).ToActionResult(this);

    /// <response code="200">The caller's own declaration for the given financial year, or null if none exists yet.</response>
    [HttpGet("mine")]
    [HasPermission(Permissions.Payroll.SelfService)]
    [ProducesResponseType(typeof(InvestmentDeclarationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine([FromQuery] string financialYear, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyInvestmentDeclarationQuery(financialYear), cancellationToken)).ToActionResult(this);
}
