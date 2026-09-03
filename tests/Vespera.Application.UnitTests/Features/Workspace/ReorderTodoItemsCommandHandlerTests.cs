using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Workspace;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class ReorderTodoItemsCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();

    private readonly IReadRepository<TodoItem> _todoItems = Substitute.For<IReadRepository<TodoItem>>();
    private readonly IWriteRepository<TodoItem> _todoItemWriter = Substitute.For<IWriteRepository<TodoItem>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public ReorderTodoItemsCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private ReorderTodoItemsCommandHandler CreateHandler() => new(
        _todoItems, _todoItemWriter, _tenantContext, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new ReorderTodoItemsCommandHandler(_todoItems, _todoItemWriter, _tenantContext, resolver);

        var result = await handler.Handle(new ReorderTodoItemsCommand([Guid.NewGuid()]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.no_employee");
    }

    [Fact]
    public async Task Handle_Should_Reorder_Items_To_Match_The_Requested_Sequence()
    {
        var first = TodoItem.Create(TenantId, EmployeeId, "First", null, TodoUrgency.Medium, 0).Value;
        var second = TodoItem.Create(TenantId, EmployeeId, "Second", null, TodoUrgency.Medium, 1).Value;
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<TodoItem>)[first, second]);

        var result = await CreateHandler().Handle(
            new ReorderTodoItemsCommand([second.Id.Value, first.Id.Value]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        second.SortOrder.Should().Be(0);
        first.SortOrder.Should().Be(1);
        _todoItemWriter.Received(1).Update(first);
        _todoItemWriter.Received(1).Update(second);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Requested_List_Omits_An_Existing_Item()
    {
        var first = TodoItem.Create(TenantId, EmployeeId, "First", null, TodoUrgency.Medium, 0).Value;
        var second = TodoItem.Create(TenantId, EmployeeId, "Second", null, TodoUrgency.Medium, 1).Value;
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<TodoItem>)[first, second]);

        var result = await CreateHandler().Handle(new ReorderTodoItemsCommand([first.Id.Value]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.reorder_mismatch");
        _todoItemWriter.DidNotReceive().Update(Arg.Any<TodoItem>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Requested_List_Contains_An_Unknown_Item()
    {
        var first = TodoItem.Create(TenantId, EmployeeId, "First", null, TodoUrgency.Medium, 0).Value;
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<TodoItem>)[first]);

        var result = await CreateHandler().Handle(new ReorderTodoItemsCommand([Guid.NewGuid()]), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.reorder_mismatch");
    }
}
