using FluentValidation.TestHelper;
using Vespera.Application.Features.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class WithdrawCandidateCommandValidatorTests
{
    private readonly WithdrawCandidateCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_CandidateId_Is_Empty()
    {
        var result = _validator.TestValidate(new WithdrawCandidateCommand(Guid.Empty, null));

        result.ShouldHaveValidationErrorFor(c => c.CandidateId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new WithdrawCandidateCommand(Guid.NewGuid(), null));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
