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

public class CreateTodoItemCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();

    private readonly IReadRepository<TodoItem> _todoItems = Substitute.For<IReadRepository<TodoItem>>();
    private readonly IWriteRepository<TodoItem> _todoItemWriter = Substitute.For<IWriteRepository<TodoItem>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public CreateTodoItemCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private CreateTodoItemCommandHandler CreateHandler() => new(
        _todoItems, _todoItemWriter, _tenantContext, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new CreateTodoItemCommandHandler(_todoItems, _todoItemWriter, _tenantContext, resolver);

        var result = await handler.Handle(new CreateTodoItemCommand("Submit timesheet", null, TodoUrgency.Medium, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.no_employee");
    }

    [Fact]
    public async Task Handle_Should_Assign_SortOrder_Zero_When_There_Are_No_Existing_Items()
    {
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<TodoItem>)[]);

        var result = await CreateHandler().Handle(new CreateTodoItemCommand("Submit timesheet", null, TodoUrgency.Medium, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _todoItemWriter.Received(1).AddAsync(Arg.Is<TodoItem>(item => item.SortOrder == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Assign_The_Next_SortOrder_After_Existing_Items()
    {
        var existing = TodoItem.Create(TenantId, EmployeeId, "Existing", null, TodoUrgency.Low, 3).Value;
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<TodoItem>)[existing]);

        var result = await CreateHandler().Handle(new CreateTodoItemCommand("New item", null, TodoUrgency.Medium, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _todoItemWriter.Received(1).AddAsync(Arg.Is<TodoItem>(item => item.SortOrder == 4), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Title_Is_Blank()
    {
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<TodoItem>)[]);

        var result = await CreateHandler().Handle(new CreateTodoItemCommand("   ", null, TodoUrgency.Medium, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _todoItemWriter.DidNotReceive().AddAsync(Arg.Any<TodoItem>(), Arg.Any<CancellationToken>());
    }
}
