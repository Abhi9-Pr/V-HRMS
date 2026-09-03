using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class GetTicketCategoriesQueryValidatorTests
{
    private readonly GetTicketCategoriesQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Not_Greater_Than_Zero()
    {
        var query = new GetTicketCategoriesQuery(new PagedRequest(0, 20));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Paging.Page);
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Is_Out_Of_Range()
    {
        var query = new GetTicketCategoriesQuery(new PagedRequest(1, 101));

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(q => q.Paging.PageSize);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Query_Is_Valid()
    {
        var query = new GetTicketCategoriesQuery(new PagedRequest(1, 20));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
