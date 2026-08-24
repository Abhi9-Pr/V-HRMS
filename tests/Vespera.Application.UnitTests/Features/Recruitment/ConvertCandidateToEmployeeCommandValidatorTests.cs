using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class ConvertCandidateToEmployeeCommandValidatorTests
{
    private readonly ConvertCandidateToEmployeeCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Employee_Code_Is_Empty() =>
        _validator.TestValidate(new ConvertCandidateToEmployeeCommand(Guid.NewGuid(), Guid.NewGuid(), "", new DateOnly(1996, 1, 1), Guid.NewGuid(), null))
            .ShouldHaveValidationErrorFor(c => c.EmployeeCode);

    [Fact]
    public void Should_Have_Error_When_Location_Id_Is_Empty() =>
        _validator.TestValidate(
                new ConvertCandidateToEmployeeCommand(Guid.NewGuid(), Guid.NewGuid(), "EMP-1", new DateOnly(1996, 1, 1), Guid.Empty, null))
            .ShouldHaveValidationErrorFor(c => c.LocationId);

    [Fact]
    public void Should_Not_Have_Errors_For_A_Valid_Command() =>
        _validator.TestValidate(
                new ConvertCandidateToEmployeeCommand(Guid.NewGuid(), Guid.NewGuid(), "EMP-1", new DateOnly(1996, 1, 1), Guid.NewGuid(), null))
            .ShouldNotHaveAnyValidationErrors();
}
