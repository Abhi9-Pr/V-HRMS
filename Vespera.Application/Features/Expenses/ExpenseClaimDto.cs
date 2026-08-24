using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

public sealed record ExpenseClaimDto(Guid Id, ExpenseClaimStatus Status, Currency SettlementCurrency, decimal Total, IReadOnlyList<ExpenseLineDto> Lines);

public sealed record ExpenseLineDto(
    Guid Id,
    string Category,
    decimal Amount,
    Currency Currency,
    decimal? ConvertedAmount,
    decimal? ExchangeRate,
    DateOnly ExpenseDate,
    string? ReceiptReference,
    string? Vendor,
    decimal? TaxAmount);
