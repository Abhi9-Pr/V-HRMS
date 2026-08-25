using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Holidays;

namespace Vespera.Application.UnitTests.Features.Holidays;

public class GetHolidaysQueryValidatorTests
{
    private readonly GetHolidaysQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var result = _validator.TestValidate(new GetHolidaysQuery(new PagedRequest { Page = 0 }, null));

        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(new GetHolidaysQuery(new PagedRequest(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
