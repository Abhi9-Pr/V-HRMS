using FluentValidation;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class CreateProxyDelegationCommandValidator : AbstractValidator<CreateProxyDelegationCommand>
{
    public CreateProxyDelegationCommandValidator()
    {
        RuleFor(x => x.DelegateEmployeeId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From);
        RuleFor(x => x.Scope).Must(s => Enum.TryParse<DelegationScope>(s, out _))
            .WithMessage($"Scope must be one of: {string.Join(", ", Enum.GetNames<DelegationScope>())}.");
    }
}
