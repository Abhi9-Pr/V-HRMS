using FluentValidation.TestHelper;
using Vespera.Application.Features.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class ConfirmAssetsRecoveredCommandValidatorTests
{
    private readonly ConfirmAssetsRecoveredCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(new ConfirmAssetsRecoveredCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new ConfirmAssetsRecoveredCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
