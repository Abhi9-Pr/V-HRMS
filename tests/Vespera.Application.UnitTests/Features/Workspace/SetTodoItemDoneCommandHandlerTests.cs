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

public class SetTodoItemDoneCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();

    private readonly IReadRepository<TodoItem> _todoItems = Substitute.For<IReadRepository<TodoItem>>();
    private readonly IWriteRepository<TodoItem> _todoItemWriter = Substitute.For<IWriteRepository<TodoItem>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public SetTodoItemDoneCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private SetTodoItemDoneCommandHandler CreateHandler() => new(
        _todoItems, _todoItemWriter, _tenantContext, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    private static TodoItem CreateItem() => TodoItem.Create(TenantId, EmployeeId, "Submit timesheet", null, TodoUrgency.Medium, 0).Value;

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new SetTodoItemDoneCommandHandler(_todoItems, _todoItemWriter, _tenantContext, resolver);

        var result = await handler.Handle(new SetTodoItemDoneCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.no_employee");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Item_Does_Not_Exist()
    {
        _todoItems.FirstOrDefaultAsync(Arg.Any<TodoItemByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((TodoItem?)null);

        var result = await CreateHandler().Handle(new SetTodoItemDoneCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.not_found");
    }

    [Fact]
    public async Task Handle_Should_Complete_The_Item()
    {
        var item = CreateItem();
        _todoItems.FirstOrDefaultAsync(Arg.Any<TodoItemByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new SetTodoItemDoneCommand(item.Id.Value, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsDone.Should().BeTrue();
        _todoItemWriter.Received(1).Update(item);
    }

    [Fact]
    public async Task Handle_Should_Reopen_The_Item()
    {
        var item = CreateItem();
        item.Complete();
        _todoItems.FirstOrDefaultAsync(Arg.Any<TodoItemByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new SetTodoItemDoneCommand(item.Id.Value, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsDone.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Done()
    {
        var item = CreateItem();
        item.Complete();
        _todoItems.FirstOrDefaultAsync(Arg.Any<TodoItemByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateHandler().Handle(new SetTodoItemDoneCommand(item.Id.Value, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _todoItemWriter.DidNotReceive().Update(Arg.Any<TodoItem>());
    }
}
