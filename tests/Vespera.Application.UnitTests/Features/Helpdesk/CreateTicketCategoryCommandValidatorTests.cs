using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class CreateTicketCategoryCommandValidatorTests
{
    private readonly CreateTicketCategoryCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateTicketCategoryCommand(string.Empty, Guid.NewGuid(), null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_DepartmentId_Is_Empty()
    {
        var command = new CreateTicketCategoryCommand("Hardware", Guid.Empty, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DepartmentId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateTicketCategoryCommand("Hardware", Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
