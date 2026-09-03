using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetBiometricDevicesQueryValidatorTests
{
    private readonly GetBiometricDevicesQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var query = new GetBiometricDevicesQuery(new PagedRequest(0, 20));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Exceeds_Two_Hundred()
    {
        var query = new GetBiometricDevicesQuery(new PagedRequest(1, 201));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor("Paging.PageSize");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var query = new GetBiometricDevicesQuery(new PagedRequest(1, 20));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
