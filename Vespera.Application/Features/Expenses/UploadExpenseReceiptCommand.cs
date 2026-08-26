using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Expenses;

public sealed record UploadExpenseReceiptCommand(
    Guid ClaimId, byte[] Content, string FileName, string? IdempotencyKey) : IRequest<Result<UploadExpenseReceiptResultDto>>, IIdempotentRequest;

/// <summary>OCR-derived field suggestions for the receipt just uploaded. Every field is editable
/// on the client before it becomes part of an <see cref="AddExpenseLineCommand"/> — never trusted
/// as-is.</summary>
public sealed record ExpenseReceiptSuggestionsDto(string? Vendor, DateOnly? ExpenseDate, decimal? TaxAmount, decimal? Amount, double Confidence);

public sealed record UploadExpenseReceiptResultDto(string ReceiptReference, ExpenseReceiptSuggestionsDto Suggestions);
