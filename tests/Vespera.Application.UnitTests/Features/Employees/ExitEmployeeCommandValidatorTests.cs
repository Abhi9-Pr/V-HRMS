using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees;

namespace Vespera.Application.UnitTests.Features.Employees;

public class ExitEmployeeCommandValidatorTests
{
    private readonly ExitEmployeeCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new ExitEmployeeCommand(Guid.NewGuid(), new DateOnly(2026, 6, 1), "Resignation"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var result = _validator.TestValidate(new ExitEmployeeCommand(Guid.NewGuid(), new DateOnly(2026, 6, 1), ""));
        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Unrecognized()
    {
        var result = _validator.TestValidate(new ExitEmployeeCommand(Guid.NewGuid(), new DateOnly(2026, 6, 1), "NotAReason"));
        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }
}
