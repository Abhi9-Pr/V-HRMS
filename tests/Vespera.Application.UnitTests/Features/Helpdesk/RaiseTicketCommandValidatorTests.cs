using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class RaiseTicketCommandValidatorTests
{
    private readonly RaiseTicketCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Subject_Is_Empty()
    {
        var command = new RaiseTicketCommand(Guid.NewGuid(), "", "Description", TicketPriority.Low, null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Subject);
    }

    [Fact]
    public void Should_Have_Error_When_Category_Id_Is_Empty()
    {
        var command = new RaiseTicketCommand(Guid.Empty, "Subject", "Description", TicketPriority.Low, null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.CategoryId);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var command = new RaiseTicketCommand(Guid.NewGuid(), "Subject", "Description", TicketPriority.Low, null);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
