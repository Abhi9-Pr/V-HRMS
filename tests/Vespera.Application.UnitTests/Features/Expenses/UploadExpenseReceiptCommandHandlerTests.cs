using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class UploadExpenseReceiptCommandHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IDocumentOcrService _ocrService = Substitute.For<IDocumentOcrService>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    private UploadExpenseReceiptCommandHandler CreateHandler() =>
        new(_expenseClaims, _fileStorage, _ocrService, CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    private ExpenseClaim CreateOwnedDraftClaim()
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, Currency.Inr);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        return claim;
    }

    [Fact]
    public async Task Handle_Should_Upload_The_Receipt_And_Return_Ocr_Suggestions()
    {
        var claim = CreateOwnedDraftClaim();
        _fileStorage.UploadAsync("receipt.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("receipts/receipt.png");
        _ocrService.ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new OcrResult(
            "text", new Dictionary<string, string> { ["vendor"] = "Uber", ["amount"] = "199.50", ["tax"] = "19.95", ["date"] = "2026-01-10" }, 0.9));

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadExpenseReceiptCommand(claim.Id.Value, [1, 2, 3], "receipt.png", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReceiptReference.Should().Be("receipts/receipt.png");
        result.Value.Suggestions.Vendor.Should().Be("Uber");
        result.Value.Suggestions.Amount.Should().Be(199.50m);
        result.Value.Suggestions.TaxAmount.Should().Be(19.95m);
        result.Value.Suggestions.ExpenseDate.Should().Be(new DateOnly(2026, 1, 10));
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Claim_Not_Found()
    {
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((ExpenseClaim?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UploadExpenseReceiptCommand(Guid.NewGuid(), [1], "receipt.png", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Claim_Belongs_To_A_Different_Employee()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);

        var handler = CreateHandler();
        var result = await handler.Handle(new UploadExpenseReceiptCommand(claim.Id.Value, [1], "receipt.png", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Claim_Is_Not_In_Draft()
    {
        var claim = CreateOwnedDraftClaim();
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        claim.Submit();

        var handler = CreateHandler();
        var result = await handler.Handle(new UploadExpenseReceiptCommand(claim.Id.Value, [1], "receipt.png", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_draft");
    }
}
