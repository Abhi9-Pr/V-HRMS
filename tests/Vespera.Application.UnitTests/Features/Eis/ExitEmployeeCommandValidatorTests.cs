using FluentValidation.TestHelper;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class ExitEmployeeCommandValidatorTests
{
    private readonly ExitEmployeeCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new ExitEmployeeCommand(Guid.Empty, new DateOnly(2026, 1, 1), EmployeeExitReason.Resignation, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ExitEmployeeCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), EmployeeExitReason.Resignation, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
