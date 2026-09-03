using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetAttendanceGridQueryValidatorTests
{
    private readonly GetAttendanceGridQueryValidator _validator = new();

    private static GetAttendanceGridQuery ValidQuery() =>
        new(null, null, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7), new PagedRequest());

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var query = ValidQuery() with { Paging = new PagedRequest(0, 20) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Exceeds_Two_Hundred()
    {
        var query = ValidQuery() with { Paging = new PagedRequest(1, 201) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.PageSize");
    }

    [Fact]
    public void Should_Have_Error_When_RangeEnd_Is_Before_RangeStart()
    {
        var query = ValidQuery() with { RangeStart = new DateOnly(2026, 1, 10), RangeEnd = new DateOnly(2026, 1, 5) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.RangeEnd);
    }

    [Fact]
    public void Should_Have_Error_When_The_Range_Spans_More_Than_Thirty_One_Days()
    {
        var query = ValidQuery() with { RangeStart = new DateOnly(2026, 1, 1), RangeEnd = new DateOnly(2026, 3, 1) };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("rangeEnd");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(ValidQuery());

        result.ShouldNotHaveAnyValidationErrors();
    }
}
