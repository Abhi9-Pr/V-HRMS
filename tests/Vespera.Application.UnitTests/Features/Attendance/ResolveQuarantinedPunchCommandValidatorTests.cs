using FluentValidation.TestHelper;
using Vespera.Application.Features.Attendance;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class ResolveQuarantinedPunchCommandValidatorTests
{
    private readonly ResolveQuarantinedPunchCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_QuarantinedBiometricPunchId_Is_Empty()
    {
        var command = new ResolveQuarantinedPunchCommand(Guid.Empty, Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.QuarantinedBiometricPunchId);
    }

    [Fact]
    public void Should_Have_Error_When_EmployeeId_Is_Empty()
    {
        var command = new ResolveQuarantinedPunchCommand(Guid.NewGuid(), Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EmployeeId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new ResolveQuarantinedPunchCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
