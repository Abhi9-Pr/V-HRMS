using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vespera.Api.Authorization;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Common;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Api.Controllers.V1.Finance;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/finance/expense-policies")]
public sealed class ExpensePoliciesController : FinanceControllerBase
{
    private readonly ISender _sender;

    public ExpensePoliciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [HasPermission(Permissions.Expenses.ManagePolicy)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpensePolicyRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateExpensePolicyCommand(
            request.Category, request.MaxAmountPerClaim, request.ReceiptRequiredAboveAmount, request.Currency,
            request.ApplicableDesignationId, request.MaxAmountSeverity, request.ReceiptRequiredSeverity, idempotencyKey);
        return (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new { id }));
    }

    [HttpGet]
    [HasPermission(Permissions.Expenses.ManagePolicy)]
    public async Task<IActionResult> Get([FromQuery] PagedRequest paging, CancellationToken cancellationToken) =>
        (await _sender.Send(new GetExpensePoliciesQuery(paging), cancellationToken)).ToActionResult(this);
}

public sealed record CreateExpensePolicyRequest(
    string Category,
    decimal MaxAmountPerClaim,
    decimal ReceiptRequiredAboveAmount,
    Currency Currency,
    Guid? ApplicableDesignationId,
    ExpensePolicySeverity MaxAmountSeverity,
    ExpensePolicySeverity ReceiptRequiredSeverity);
