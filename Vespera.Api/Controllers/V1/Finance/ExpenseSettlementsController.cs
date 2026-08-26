using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Expenses;

namespace Vespera.Api.Controllers.V1.Finance;

/// <summary>The 11a -> Phase 10 payroll integration point: pushes an approved expense claim's
/// total into a Draft payroll run as a reimbursement component.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/expense-settlements")]
public sealed class ExpenseSettlementsController : FinanceControllerBase
{
    private readonly ISender _sender;

    public ExpenseSettlementsController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="204">Settled — the claim is now Reimbursed and the payroll run carries a new reimbursement line.</response>
    [HttpPost]
    [HasPermission(Permissions.Expenses.Settle)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Settle(
        [FromBody] SettleExpenseClaimRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new SettleExpenseClaimCommand(request.ExpenseClaimId, request.PayrollRunId, idempotencyKey), cancellationToken))
            .ToActionResult(this);
}

public sealed record SettleExpenseClaimRequest(Guid ExpenseClaimId, Guid PayrollRunId);
