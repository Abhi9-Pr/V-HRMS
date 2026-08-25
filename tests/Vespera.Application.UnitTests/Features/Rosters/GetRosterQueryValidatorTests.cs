using FluentValidation.TestHelper;
using Vespera.Application.Features.Rosters;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class GetRosterQueryValidatorTests
{
    private readonly GetRosterQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RangeEnd_Before_RangeStart()
    {
        var query = new GetRosterQuery(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1), null, null);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.RangeEnd);
    }

    [Fact]
    public void Should_Have_Error_When_Range_Exceeds_Max_Days()
    {
        var start = new DateOnly(2026, 1, 1);
        var query = new GetRosterQuery(start, start.AddDays(400), null, null);

        var result = _validator.TestValidate(query);

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var start = new DateOnly(2026, 1, 1);
        var query = new GetRosterQuery(start, start.AddDays(6), null, null);

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
