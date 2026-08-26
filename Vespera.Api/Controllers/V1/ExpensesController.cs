using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.ValueObjects;

namespace Vespera.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly ISender _sender;

    public ExpensesController(ISender sender)
    {
        _sender = sender;
    }

    /// <response code="200">The new (Draft) claim's id.</response>
    [HttpPost("claims")]
    [HasPermission(Permissions.Expenses.Submit)]
    [ProducesResponseType(typeof(OpenExpenseClaimResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> OpenClaim(
        [FromBody] OpenClaimRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new OpenExpenseClaimCommand(request.SettlementCurrency, idempotencyKey), cancellationToken))
            .ToActionResult(this, id => Ok(new OpenExpenseClaimResponse(id)));

    /// <summary>Uploads a receipt and runs OCR over it, returning editable field suggestions — the
    /// client pre-fills the line form with these but the values submitted to
    /// <see cref="AddLine"/> are whatever the user confirms/edits, never trusted as-is.</summary>
    /// <response code="200">The storage reference and OCR suggestions.</response>
    [HttpPost("claims/{claimId:guid}/receipts")]
    [HasPermission(Permissions.Expenses.Submit)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadExpenseReceiptResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadReceipt(
        Guid claimId, IFormFile file, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var command = new UploadExpenseReceiptCommand(claimId, buffer.ToArray(), file.FileName, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <response code="204">Line added.</response>
    [HttpPost("claims/{claimId:guid}/lines")]
    [HasPermission(Permissions.Expenses.Submit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddLine(
        Guid claimId, [FromBody] AddLineRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new AddExpenseLineCommand(
            claimId, request.Category, request.Amount, request.Currency, request.ExpenseDate, request.ReceiptReference,
            request.Vendor, request.TaxAmount, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this);
    }

    /// <response code="200">Submitted — any non-blocking policy warnings.</response>
    [HttpPost("claims/{claimId:guid}/submit")]
    [HasPermission(Permissions.Expenses.Submit)]
    [ProducesResponseType(typeof(SubmitExpenseClaimResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitClaim(
        Guid claimId, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken) =>
        (await _sender.Send(new SubmitExpenseClaimCommand(claimId, idempotencyKey), cancellationToken)).ToActionResult(this);

    /// <response code="204">Decision recorded.</response>
    /// <response code="403">The caller is not the claim's current approver.</response>
    [HttpPost("claims/{claimId:guid}/decision")]
    [HasPermission(Permissions.Expenses.Approve)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DecideApproval(
        Guid claimId, [FromBody] DecideApprovalRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        (await _sender.Send(new DecideExpenseApprovalCommand(claimId, request.Approved, request.Comment, idempotencyKey), cancellationToken))
            .ToActionResult(this);

    /// <response code="200">A page of the caller's own claims.</response>
    [HttpGet("claims")]
    [HasPermission(Permissions.Expenses.Submit)]
    [ProducesResponseType(typeof(PagedResult<ExpenseClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyClaims([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetMyExpenseClaimsQuery(paging), cancellationToken)).ToActionResult(this);

    /// <response code="200">The claim.</response>
    /// <response code="404">No such claim.</response>
    [HttpGet("claims/{id:guid}")]
    [HasPermission(Permissions.Expenses.Submit)]
    [ProducesResponseType(typeof(ExpenseClaimDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetClaimById(Guid id, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetExpenseClaimByIdQuery(id), cancellationToken)).ToActionResult(this);

    /// <response code="200">A page of claims pending the caller's approval.</response>
    [HttpGet("approvals/pending")]
    [HasPermission(Permissions.Expenses.Approve)]
    [ProducesResponseType(typeof(PagedResult<ExpenseClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetPendingExpenseApprovalsQuery(paging), cancellationToken)).ToActionResult(this);
}

public sealed record OpenClaimRequest(Currency SettlementCurrency);

public sealed record AddLineRequest(
    string Category, decimal Amount, Currency Currency, DateOnly ExpenseDate, string? ReceiptReference, string? Vendor, decimal? TaxAmount);

public sealed record DecideApprovalRequest(bool Approved, string? Comment);

public sealed record OpenExpenseClaimResponse(Guid Id);
