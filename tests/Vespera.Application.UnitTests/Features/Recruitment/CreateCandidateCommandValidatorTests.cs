using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class CreateCandidateCommandValidatorTests
{
    private readonly CreateCandidateCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_JobRequisitionId_Is_Empty()
    {
        var command = new CreateCandidateCommand(Guid.Empty, "Jordan Lee", "jordan.lee@example.com", "+14155552671", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.JobRequisitionId);
    }

    [Fact]
    public void Should_Have_Error_When_FullName_Is_Empty()
    {
        var command = new CreateCandidateCommand(Guid.NewGuid(), string.Empty, "jordan.lee@example.com", "+14155552671", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FullName);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var command = new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", string.Empty, "+14155552671", null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Phone_Is_Empty()
    {
        var command = new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", "jordan.lee@example.com", string.Empty, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Phone);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new CreateCandidateCommand(Guid.NewGuid(), "Jordan Lee", "jordan.lee@example.com", "+14155552671", null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
