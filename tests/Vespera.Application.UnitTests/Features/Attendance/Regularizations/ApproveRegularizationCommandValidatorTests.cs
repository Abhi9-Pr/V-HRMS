using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance.Regularizations;

namespace Vespera.Application.UnitTests.Features.Attendance.Regularizations;

public class ApproveRegularizationCommandValidatorTests
{
    private readonly ApproveRegularizationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_RequestId_Is_Empty()
    {
        var command = new ApproveRegularizationCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.RequestId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ApproveRegularizationCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
