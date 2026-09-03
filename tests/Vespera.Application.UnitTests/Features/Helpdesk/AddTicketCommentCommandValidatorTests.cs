using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class AddTicketCommentCommandValidatorTests
{
    private readonly AddTicketCommentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TicketId_Is_Empty()
    {
        var command = new AddTicketCommentCommand(Guid.Empty, "Body", false, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Fact]
    public void Should_Have_Error_When_Body_Is_Empty()
    {
        var command = new AddTicketCommentCommand(Guid.NewGuid(), string.Empty, false, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Body);
    }

    [Fact]
    public void Should_Have_Error_When_Body_Exceeds_Max_Length()
    {
        var command = new AddTicketCommentCommand(Guid.NewGuid(), new string('a', 4097), false, null, null, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Body);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new AddTicketCommentCommand(Guid.NewGuid(), "Body", true, Guid.NewGuid(), ["file.png"], null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
