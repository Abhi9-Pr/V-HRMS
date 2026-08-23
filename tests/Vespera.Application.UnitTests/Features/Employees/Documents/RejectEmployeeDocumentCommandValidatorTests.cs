using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees.Documents;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class RejectEmployeeDocumentCommandValidatorTests
{
    private readonly RejectEmployeeDocumentCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new RejectEmployeeDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "Blurry scan"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var result = _validator.TestValidate(new RejectEmployeeDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }
}
