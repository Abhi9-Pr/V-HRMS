using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class AssignTicketCommandValidatorTests
{
    private readonly AssignTicketCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TicketId_Is_Empty()
    {
        var command = new AssignTicketCommand(Guid.Empty, Guid.NewGuid(), null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new AssignTicketCommand(Guid.NewGuid(), Guid.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
