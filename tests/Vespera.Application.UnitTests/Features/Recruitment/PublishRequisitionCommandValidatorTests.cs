using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class PublishRequisitionCommandValidatorTests
{
    private readonly PublishRequisitionCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RequisitionId_Is_Empty()
    {
        var result = _validator.TestValidate(new PublishRequisitionCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.RequisitionId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new PublishRequisitionCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
