using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed record CreateExpensePolicyCommand(
    string Category,
    decimal MaxAmountPerClaim,
    decimal ReceiptRequiredAboveAmount,
    Currency Currency,
    Guid? ApplicableDesignationId,
    ExpensePolicySeverity MaxAmountSeverity,
    ExpensePolicySeverity ReceiptRequiredSeverity,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
