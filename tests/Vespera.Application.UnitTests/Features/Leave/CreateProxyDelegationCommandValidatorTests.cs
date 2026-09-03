using FluentValidation.TestHelper;
using Vespera.Application.Features.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class CreateProxyDelegationCommandValidatorTests
{
    private readonly CreateProxyDelegationCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_DelegateEmployeeId_Is_Empty()
    {
        var result = _validator.TestValidate(new CreateProxyDelegationCommand(Guid.Empty, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "All"));

        result.ShouldHaveValidationErrorFor(c => c.DelegateEmployeeId);
    }

    [Fact]
    public void Should_Have_Error_When_To_Is_Before_From()
    {
        var result = _validator.TestValidate(new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 1), "All"));

        result.ShouldHaveValidationErrorFor(c => c.To);
    }

    [Fact]
    public void Should_Have_Error_When_Scope_Is_Not_A_Known_DelegationScope()
    {
        var result = _validator.TestValidate(new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "NotARealScope"));

        result.ShouldHaveValidationErrorFor(c => c.Scope);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var result = _validator.TestValidate(new CreateProxyDelegationCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10), "LeaveApprovals"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
