using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.RotationPatterns;

namespace Vespera.Application.UnitTests.Features.RotationPatterns;

public class GetRotationPatternsQueryValidatorTests
{
    private readonly GetRotationPatternsQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var result = _validator.TestValidate(new GetRotationPatternsQuery(new PagedRequest { Page = 0 }));

        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var result = _validator.TestValidate(new GetRotationPatternsQuery(new PagedRequest()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
