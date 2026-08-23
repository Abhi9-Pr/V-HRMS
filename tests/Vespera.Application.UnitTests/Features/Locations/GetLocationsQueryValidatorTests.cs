using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Locations;

namespace Vespera.Application.UnitTests.Features.Locations;

public class GetLocationsQueryValidatorTests
{
    private readonly GetLocationsQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var query = new GetLocationsQuery(new PagedRequest { Page = 0, PageSize = 20 });

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Exceeds_Maximum()
    {
        var query = new GetLocationsQuery(new PagedRequest { Page = 1, PageSize = 500 });

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.PageSize");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var query = new GetLocationsQuery(new PagedRequest { Page = 1, PageSize = 20 });

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
