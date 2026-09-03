using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class ReleaseLicenseSeatCommandValidatorTests
{
    private readonly ReleaseLicenseSeatCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AllocationId_Is_Empty()
    {
        var result = _validator.TestValidate(new ReleaseLicenseSeatCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.AllocationId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new ReleaseLicenseSeatCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
