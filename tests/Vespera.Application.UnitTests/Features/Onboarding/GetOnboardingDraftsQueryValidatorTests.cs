using FluentValidation.TestHelper;
using Vespera.Application.Common;
using Vespera.Application.Features.Onboarding;

namespace Vespera.Application.UnitTests.Features.Onboarding;

public class GetOnboardingDraftsQueryValidatorTests
{
    private readonly GetOnboardingDraftsQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Page_Is_Zero()
    {
        var result = _validator.TestValidate(new GetOnboardingDraftsQuery(new PagedRequest(0, 20, null, false)));

        result.ShouldHaveValidationErrorFor(q => q.Paging.Page);
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Exceeds_The_Maximum()
    {
        var result = _validator.TestValidate(new GetOnboardingDraftsQuery(new PagedRequest(1, 201, null, false)));

        result.ShouldHaveValidationErrorFor(q => q.Paging.PageSize);
    }

    [Fact]
    public void Should_Have_Error_When_PageSize_Is_Zero()
    {
        var result = _validator.TestValidate(new GetOnboardingDraftsQuery(new PagedRequest(1, 0, null, false)));

        result.ShouldHaveValidationErrorFor(q => q.Paging.PageSize);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Page()
    {
        var result = _validator.TestValidate(new GetOnboardingDraftsQuery(new PagedRequest(1, 20, null, false)));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
