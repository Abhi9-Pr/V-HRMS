using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class RevokeProxyDelegationCommandValidatorTests
{
    private readonly RevokeProxyDelegationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_DelegationId_Is_Empty()
    {
        var result = _validator.TestValidate(new RevokeProxyDelegationCommand(Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.DelegationId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new RevokeProxyDelegationCommand(Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
