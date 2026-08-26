using FluentValidation;

namespace Vespera.Application.Features.Leave;

public sealed class RevokeProxyDelegationCommandValidator : AbstractValidator<RevokeProxyDelegationCommand>
{
    public RevokeProxyDelegationCommandValidator()
    {
        RuleFor(x => x.DelegationId).NotEmpty();
    }
}
