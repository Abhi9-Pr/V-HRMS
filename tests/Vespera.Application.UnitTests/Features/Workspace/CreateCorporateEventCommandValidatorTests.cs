using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class CreateCorporateEventCommandValidatorTests
{
    private static readonly DateTimeOffset StartsAt = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly CreateCorporateEventCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty()
    {
        var result = _validator.TestValidate(CreateCommand() with { Title = string.Empty });

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Title_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(CreateCommand() with { Title = new string('a', 257) });

        result.ShouldHaveValidationErrorFor(command => command.Title);
    }

    [Fact]
    public void Should_Have_Error_When_Description_Is_Empty()
    {
        var result = _validator.TestValidate(CreateCommand() with { Description = string.Empty });

        result.ShouldHaveValidationErrorFor(command => command.Description);
    }

    [Fact]
    public void Should_Have_Error_When_LocationText_Is_Empty()
    {
        var result = _validator.TestValidate(CreateCommand() with { LocationText = string.Empty });

        result.ShouldHaveValidationErrorFor(command => command.LocationText);
    }

    [Fact]
    public void Should_Have_Error_When_LocationText_Exceeds_Max_Length()
    {
        var result = _validator.TestValidate(CreateCommand() with { LocationText = new string('a', 257) });

        result.ShouldHaveValidationErrorFor(command => command.LocationText);
    }

    [Fact]
    public void Should_Have_Error_When_EndsAt_Is_Not_After_StartsAt()
    {
        var result = _validator.TestValidate(CreateCommand() with { EndsAt = StartsAt });

        result.ShouldHaveValidationErrorFor(command => command.EndsAt);
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(CreateCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateCorporateEventCommand CreateCommand() =>
        new("All-hands", "Quarterly all-hands", StartsAt, StartsAt.AddHours(2), "HQ", null);
}
