using FluentValidation;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard;

public sealed class SaveDashboardLayoutCommandValidator : AbstractValidator<SaveDashboardLayoutCommand>
{
    public SaveDashboardLayoutCommandValidator()
    {
        RuleFor(command => command.Widgets).NotNull();

        RuleForEach(command => command.Widgets).ChildRules(widget =>
        {
            widget.RuleFor(w => w.WidgetKey).NotEmpty().MaximumLength(64);
            widget.RuleFor(w => w.Size).Must(size => Enum.TryParse<WidgetSize>(size, ignoreCase: true, out _))
                .WithMessage("Size must be one of: " + string.Join(", ", Enum.GetNames<WidgetSize>()));
        });
    }
}
