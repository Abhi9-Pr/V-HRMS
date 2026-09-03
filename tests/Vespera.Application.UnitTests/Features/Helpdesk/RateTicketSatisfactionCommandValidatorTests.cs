using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class RateTicketSatisfactionCommandValidatorTests
{
    private readonly RateTicketSatisfactionCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_TicketId_Is_Empty()
    {
        var command = new RateTicketSatisfactionCommand(Guid.Empty, 5, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TicketId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Should_Have_Error_When_Rating_Is_Out_Of_Range(int rating)
    {
        var command = new RateTicketSatisfactionCommand(Guid.NewGuid(), rating, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Rating);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new RateTicketSatisfactionCommand(Guid.NewGuid(), 5, null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
