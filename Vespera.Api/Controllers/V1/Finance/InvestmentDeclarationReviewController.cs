using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Payroll;

namespace Vespera.Api.Controllers.V1.Finance;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/investment-declarations")]
public sealed class InvestmentDeclarationReviewController : FinanceControllerBase
{
    private readonly ISender _sender;

    public InvestmentDeclarationReviewController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">Every Submitted declaration awaiting review, paged.</response>
    [HttpGet("review-queue")]
    [HasPermission(Permissions.Payroll.Read)]
    [ProducesResponseType(typeof(PagedResult<InvestmentDeclarationQueueItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReviewQueue([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetInvestmentDeclarationReviewQueueQuery(paging), cancellationToken)).ToActionResult(this);

    /// <summary>Reviewing the last still-Pending line automatically verifies the declaration.</summary>
    /// <response code="204">Reviewed.</response>
    [HttpPost("{id:guid}/lines/{lineIndex:int}/review")]
    [HasPermission(Permissions.Payroll.Write)]
    public async Task<IActionResult> ReviewLine(
        Guid id, int lineIndex, [FromBody] ReviewInvestmentDeclarationLineRequest request, CancellationToken cancellationToken) =>
        (await _sender.Send(new ReviewInvestmentDeclarationLineCommand(id, lineIndex, request.Approved, request.Comment), cancellationToken))
            .ToActionResult(this);
}

public sealed record ReviewInvestmentDeclarationLineRequest(bool Approved, string? Comment);
