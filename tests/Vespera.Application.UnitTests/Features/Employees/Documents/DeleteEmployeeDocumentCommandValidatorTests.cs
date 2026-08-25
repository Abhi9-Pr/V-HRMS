using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees.Documents;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class DeleteEmployeeDocumentCommandValidatorTests
{
    private readonly DeleteEmployeeDocumentCommandValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(new DeleteEmployeeDocumentCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_DocumentId_Is_Empty()
    {
        var result = _validator.TestValidate(new DeleteEmployeeDocumentCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(command => command.DocumentId);
    }
}
