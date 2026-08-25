using FluentValidation.TestHelper;
using Vespera.Application.Features.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class EndReportingRelationshipCommandValidatorTests
{
    private readonly EndReportingRelationshipCommandValidator _validator = new();

    private static EndReportingRelationshipCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 6, 30));

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
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { Id = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.Id);
    }
}
