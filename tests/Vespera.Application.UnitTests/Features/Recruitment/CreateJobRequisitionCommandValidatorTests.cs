using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateJobRequisitionCommandValidatorTests
{
    private readonly CreateJobRequisitionCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Title_Is_Empty() =>
        _validator.TestValidate(new CreateJobRequisitionCommand("", Guid.NewGuid(), 1, null)).ShouldHaveValidationErrorFor(c => c.Title);

    [Fact]
    public void Should_Have_Error_When_Openings_Count_Is_Not_Positive() =>
        _validator.TestValidate(new CreateJobRequisitionCommand("Engineer", Guid.NewGuid(), 0, null))
            .ShouldHaveValidationErrorFor(c => c.OpeningsCount);

    [Fact]
    public void Should_Not_Have_Errors_For_A_Valid_Command() =>
        _validator.TestValidate(new CreateJobRequisitionCommand("Engineer", Guid.NewGuid(), 2, null)).ShouldNotHaveAnyValidationErrors();
}
