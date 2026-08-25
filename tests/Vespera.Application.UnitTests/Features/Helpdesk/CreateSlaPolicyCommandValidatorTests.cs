using FluentValidation.TestHelper;
using Vespera.Application.Features.Helpdesk;

namespace Vespera.Application.UnitTests.Features.Helpdesk;

public class CreateSlaPolicyCommandValidatorTests
{
    private readonly CreateSlaPolicyCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var command = new CreateSlaPolicyCommand("", 2, 24, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Resolution_Time_Is_Shorter_Than_Response_Time()
    {
        var command = new CreateSlaPolicyCommand("Standard", 24, 2, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.ResolutionTimeHours);
    }

    [Fact]
    public void Should_Have_Error_When_Business_Hours_End_Is_Not_After_Start()
    {
        var command = new CreateSlaPolicyCommand("Standard", 2, 24, new TimeOnly(18, 0), new TimeOnly(9, 0), null);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(c => c.BusinessHoursEnd);
    }

    [Fact]
    public void Should_Not_Have_Error_For_A_Valid_Command()
    {
        var command = new CreateSlaPolicyCommand("Standard", 2, 24, new TimeOnly(9, 0), new TimeOnly(18, 0), null);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }
}
