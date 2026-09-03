using FluentValidation.TestHelper;
using Vespera.Application.Features.Assets;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetAssetByIdQueryValidatorTests
{
    private readonly GetAssetByIdQueryValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new GetAssetByIdQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Set()
    {
        var result = _validator.TestValidate(new GetAssetByIdQuery(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
