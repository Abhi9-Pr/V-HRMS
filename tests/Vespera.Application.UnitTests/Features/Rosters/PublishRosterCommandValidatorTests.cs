using FluentValidation.TestHelper;
using Vespera.Application.Features.Rosters;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class PublishRosterCommandValidatorTests
{
    private readonly PublishRosterCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeIds_Is_Empty()
    {
        var command = new PublishRosterCommand([], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeIds);
    }

    [Fact]
    public void Should_Have_Error_When_RangeEnd_Before_RangeStart()
    {
        var command = new PublishRosterCommand([Guid.NewGuid()], new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RangeEnd);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new PublishRosterCommand([Guid.NewGuid()], new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
