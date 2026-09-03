using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateBlackoutPeriodCommandValidatorTests
{
    private readonly CreateBlackoutPeriodCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_To_Is_Before_From()
    {
        var result = _validator.TestValidate(new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 25), new DateOnly(2026, 12, 20), "Festival", null));

        result.ShouldHaveValidationErrorFor(c => c.To);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25), string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CreateBlackoutPeriodCommand(new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 25), "Festival", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
