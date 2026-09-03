using FluentValidation.TestHelper;
using Vespera.Application.Features.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class ReorderTodoItemsCommandValidatorTests
{
    private readonly ReorderTodoItemsCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_OrderedTodoItemIds_Is_Null()
    {
        var result = _validator.TestValidate(new ReorderTodoItemsCommand(null!));

        result.ShouldHaveValidationErrorFor(command => command.OrderedTodoItemIds);
    }

    [Fact]
    public void Should_Have_Error_When_An_Item_Id_Is_Empty()
    {
        var result = _validator.TestValidate(new ReorderTodoItemsCommand([Guid.NewGuid(), Guid.Empty]));

        result.ShouldHaveValidationErrorFor("OrderedTodoItemIds[1]");
    }

    [Fact]
    public void Should_Not_Have_Errors_When_Valid()
    {
        var result = _validator.TestValidate(new ReorderTodoItemsCommand([Guid.NewGuid(), Guid.NewGuid()]));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
