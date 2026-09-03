using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class UploadTicketAttachmentCommandValidatorTests
{
    private readonly UploadTicketAttachmentCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TicketId_Is_Empty()
    {
        var command = new UploadTicketAttachmentCommand(Guid.Empty, [1, 2, 3], "photo.png", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Is_Empty()
    {
        var command = new UploadTicketAttachmentCommand(Guid.NewGuid(), [1, 2, 3], string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_FileName_Exceeds_Max_Length()
    {
        var command = new UploadTicketAttachmentCommand(Guid.NewGuid(), [1, 2, 3], new string('a', 257), null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FileName);
    }

    [Fact]
    public void Should_Have_Error_When_Content_Is_Empty()
    {
        var command = new UploadTicketAttachmentCommand(Guid.NewGuid(), [], "photo.png", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Content);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new UploadTicketAttachmentCommand(Guid.NewGuid(), [1, 2, 3], "photo.png", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
