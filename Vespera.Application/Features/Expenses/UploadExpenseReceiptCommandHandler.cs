using System.Globalization;
using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class UploadExpenseReceiptCommandHandler : IRequestHandler<UploadExpenseReceiptCommand, Result<UploadExpenseReceiptResultDto>>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IFileStorage _fileStorage;
    private readonly IDocumentOcrService _ocrService;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public UploadExpenseReceiptCommandHandler(
        IReadRepository<ExpenseClaim> expenseClaims, IFileStorage fileStorage, IDocumentOcrService ocrService,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _fileStorage = fileStorage;
        _ocrService = ocrService;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<UploadExpenseReceiptResultDto>> Handle(
        UploadExpenseReceiptCommand request, CancellationToken cancellationToken)
    {
        var claim = await _expenseClaims.FirstOrDefaultAsync(
            new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.ClaimId)), cancellationToken);

        if (claim is null)
        {
            return Result.Failure<UploadExpenseReceiptResultDto>(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null || claim.EmployeeId != employeeId.Value)
        {
            return Result.Failure<UploadExpenseReceiptResultDto>(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        if (claim.Status != ExpenseClaimStatus.Draft)
        {
            return Result.Failure<UploadExpenseReceiptResultDto>(
                Error.Conflict("expense_claim.not_draft", "Receipts can only be uploaded while the claim is in Draft."));
        }

        var receiptReference = await _fileStorage.UploadAsync(request.FileName, new MemoryStream(request.Content), cancellationToken);
        var ocrResult = await _ocrService.ExtractAsync(new MemoryStream(request.Content), cancellationToken);

        return Result.Success(new UploadExpenseReceiptResultDto(receiptReference, BuildSuggestions(ocrResult)));
    }

    private static ExpenseReceiptSuggestionsDto BuildSuggestions(OcrResult ocrResult)
    {
        var fields = new Dictionary<string, string>(ocrResult.Fields, StringComparer.OrdinalIgnoreCase);

        string? vendor = fields.TryGetValue("vendor", out var vendorValue) && !string.IsNullOrWhiteSpace(vendorValue) ? vendorValue : null;

        DateOnly? expenseDate = fields.TryGetValue("date", out var dateValue)
            && DateOnly.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : null;

        decimal? taxAmount = fields.TryGetValue("tax", out var taxValue)
            && decimal.TryParse(taxValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedTax)
            ? parsedTax
            : null;

        decimal? amount = fields.TryGetValue("amount", out var amountValue)
            && decimal.TryParse(amountValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount)
            ? parsedAmount
            : null;

        return new ExpenseReceiptSuggestionsDto(vendor, expenseDate, taxAmount, amount, ocrResult.Confidence);
    }
}
