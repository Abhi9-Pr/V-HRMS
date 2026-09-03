using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class AddPipelineStageCommandValidatorTests
{
    private readonly AddPipelineStageCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RequisitionId_Is_Empty()
    {
        var result = _validator.TestValidate(new AddPipelineStageCommand(Guid.Empty, "Screening", null));

        result.ShouldHaveValidationErrorFor(c => c.RequisitionId);
    }

    [Fact]
    public void Should_Have_Error_When_StageName_Is_Empty()
    {
        var result = _validator.TestValidate(new AddPipelineStageCommand(Guid.NewGuid(), string.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.StageName);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new AddPipelineStageCommand(Guid.NewGuid(), "Screening", null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
