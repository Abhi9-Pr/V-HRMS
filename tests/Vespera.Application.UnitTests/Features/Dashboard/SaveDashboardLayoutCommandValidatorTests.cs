using FluentValidation.TestHelper;
using Vespera.Application.Features.Dashboard;

namespace Vespera.Application.UnitTests.Features.Dashboard;

public class SaveDashboardLayoutCommandValidatorTests
{
    private readonly SaveDashboardLayoutCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Widgets_Is_Null()
    {
        var command = new SaveDashboardLayoutCommand(null!, null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Widgets);
    }

    [Fact]
    public void Should_Have_Error_When_A_Widget_Key_Is_Empty()
    {
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput(string.Empty, 0, true, "Medium")], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Widgets[0].WidgetKey");
    }

    [Fact]
    public void Should_Have_Error_When_A_Widget_Size_Is_Not_A_Known_Value()
    {
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput("shift-tracker", 0, true, "Huge")], null);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Widgets[0].Size");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        var command = new SaveDashboardLayoutCommand([new WidgetPreferenceInput("shift-tracker", 0, true, "Medium")], null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
