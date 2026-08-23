using FluentValidation.TestHelper;
using Vespera.Application.Features.Employees;

namespace Vespera.Application.UnitTests.Features.Employees;

public class CreateEmployeeCommandValidatorTests
{
    private readonly CreateEmployeeCommandValidator _validator = new();

    private static CreateEmployeeCommand ValidCommand() => new(
        "EMP-900", "Grace", "Hopper", "grace.hopper@vespera.test", "+14155552672",
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Code_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { Code = "" });
        result.ShouldHaveValidationErrorFor(command => command.Code);
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { FirstName = "" });
        result.ShouldHaveValidationErrorFor(command => command.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_LastName_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { LastName = "" });
        result.ShouldHaveValidationErrorFor(command => command.LastName);
    }

    [Fact]
    public void Should_Have_Error_When_DateOfBirth_Is_Not_Before_DateOfJoining()
    {
        var result = _validator.TestValidate(ValidCommand() with { DateOfBirth = new DateOnly(2026, 1, 15) });
        result.ShouldHaveValidationErrorFor(command => command.DateOfBirth);
    }

    [Fact]
    public void Should_Have_Error_When_DepartmentId_Is_Empty()
    {
        var result = _validator.TestValidate(ValidCommand() with { DepartmentId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(command => command.DepartmentId);
    }
}
