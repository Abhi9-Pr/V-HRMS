using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class PublishAnnouncementCommandValidatorTests
{
    private readonly PublishAnnouncementCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AnnouncementId_Is_Empty()
    {
        var result = _validator.TestValidate(new PublishAnnouncementCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.AnnouncementId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new PublishAnnouncementCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
