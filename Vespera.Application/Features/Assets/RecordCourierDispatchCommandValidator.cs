using FluentValidation;

namespace Vespera.Application.Features.Assets;

public sealed class RecordCourierDispatchCommandValidator : AbstractValidator<RecordCourierDispatchCommand>
{
    public RecordCourierDispatchCommandValidator()
    {
        RuleFor(command => command.RecoveryId).NotEmpty();
        RuleFor(command => command.Carrier).NotEmpty().MaximumLength(128);
        RuleFor(command => command.TrackingReference).NotEmpty().MaximumLength(128);
    }
}
