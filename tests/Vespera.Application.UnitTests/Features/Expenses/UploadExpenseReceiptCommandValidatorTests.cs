using FluentValidation.TestHelper;
using Vespera.Application.Features.Expenses;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class UploadExpenseReceiptCommandValidatorTests
{
    private readonly UploadExpenseReceiptCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ClaimId_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadExpenseReceiptCommand(Guid.Empty, [1, 2, 3], "receipt.png", null));

        result.ShouldHaveValidationErrorFor(c => c.ClaimId);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadExpenseReceiptCommand(Guid.NewGuid(), [], "receipt.png", null));

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var result = _validator.TestValidate(new UploadExpenseReceiptCommand(Guid.NewGuid(), [1, 2, 3], string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(new UploadExpenseReceiptCommand(Guid.NewGuid(), [1, 2, 3], new string('a', 257), null));

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new UploadExpenseReceiptCommand(Guid.NewGuid(), [1, 2, 3], "receipt.png", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
