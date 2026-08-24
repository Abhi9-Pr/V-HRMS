using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

public sealed record AddExpenseLineCommand(
    Guid ClaimId,
    string Category,
    decimal Amount,
    Currency Currency,
    DateOnly ExpenseDate,
    string? ReceiptReference,
    string? Vendor,
    decimal? TaxAmount,
    string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
