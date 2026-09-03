using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class SetAnnouncementPinnedCommandValidatorTests
{
    private readonly SetAnnouncementPinnedCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_AnnouncementId_Is_Empty()
    {
        var result = _validator.TestValidate(new SetAnnouncementPinnedCommand(Guid.Empty, true));

        result.ShouldHaveValidationErrorFor(command => command.AnnouncementId);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new SetAnnouncementPinnedCommand(Guid.NewGuid(), true));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
