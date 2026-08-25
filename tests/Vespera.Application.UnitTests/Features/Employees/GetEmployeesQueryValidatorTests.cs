using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Employees;

namespace Vespera.Application.UnitTests.Features.Employees;

public class GetEmployeesQueryValidatorTests
{
    private readonly GetEmployeesQueryValidator _validator = new();

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Query()
    {
        var result = _validator.TestValidate(new GetEmployeesQuery(new PagedRequest(1, 20, null, false)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Page_Is_Less_Than_One()
    {
        var result = _validator.TestValidate(new GetEmployeesQuery(new PagedRequest(0, 20, null, false)));
        result.ShouldHaveValidationErrorFor("Paging.Page");
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Exceeds_The_Maximum()
    {
        var result = _validator.TestValidate(new GetEmployeesQuery(new PagedRequest(1, 500, null, false)));
        result.ShouldHaveValidationErrorFor("Paging.PageSize");
    }
}
