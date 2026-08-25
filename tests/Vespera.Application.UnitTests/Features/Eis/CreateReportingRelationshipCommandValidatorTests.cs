using FluentValidation.TestHelper;
using Vespera.Application.Features.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class CreateReportingRelationshipCommandValidatorTests
{
    private readonly CreateReportingRelationshipCommandValidator _validator = new();

    private static CreateReportingRelationshipCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { EmployeeId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.EmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_ManagerId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { ManagerId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.ManagerId);
    }

    [Fact]
    public void Should_Have_Error_When_ValidTo_Is_Before_ValidFrom()
    {
        var result = _validator.TestValidate(ValidCommand() with { ValidFrom = new DateOnly(2026, 6, 1), ValidTo = new DateOnly(2026, 1, 1) });
        result.ShouldHaveValidationErrorFor(command => command.ValidTo);
    }

    [Fact]
    public void Should_Not_Have_Error_When_ValidTo_Is_Null()
    {
        var result = _validator.TestValidate(ValidCommand() with { ValidTo = null });
        result.ShouldNotHaveValidationErrorFor(command => command.ValidTo);
    }
}
