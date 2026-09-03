using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class RespondToEventRsvpCommandValidatorTests
{
    private readonly RespondToEventRsvpCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CorporateEventId_Is_Empty()
    {
        var result = _validator.TestValidate(new RespondToEventRsvpCommand(Guid.Empty, RsvpResponse.Yes));

        result.ShouldHaveValidationErrorFor(command => command.CorporateEventId);
    }

    [Fact]
    public void Should_Have_Error_When_Response_Is_Not_A_Defined_Enum_Value()
    {
        var result = _validator.TestValidate(new RespondToEventRsvpCommand(Guid.NewGuid(), (RsvpResponse)999));

        result.ShouldHaveValidationErrorFor(command => command.Response);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new RespondToEventRsvpCommand(Guid.NewGuid(), RsvpResponse.Maybe));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
