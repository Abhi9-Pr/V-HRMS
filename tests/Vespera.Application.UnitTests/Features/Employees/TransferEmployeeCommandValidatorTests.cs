using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees;

namespace Vespera.Application.UnitTests.Features.Employees;

public class TransferEmployeeCommandValidatorTests
{
    private readonly TransferEmployeeCommandValidator _validator = new();

    private static TransferEmployeeCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), "Transfer");

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_DepartmentId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { DepartmentId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.DepartmentId);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Unrecognized()
    {
        var result = _validator.TestValidate(ValidCommand() with { Reason = "NotAReason" });
        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }

    [Fact]
    public void Should_Have_Error_When_Reason_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { Reason = "" });
        result.ShouldHaveValidationErrorFor(command => command.Reason);
    }
}
