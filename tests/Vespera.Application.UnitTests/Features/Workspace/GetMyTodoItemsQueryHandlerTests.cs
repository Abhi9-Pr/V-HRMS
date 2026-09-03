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

public class GetMyTodoItemsQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();

    private readonly IReadRepository<TodoItem> _todoItems = Substitute.For<IReadRepository<TodoItem>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public GetMyTodoItemsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetMyTodoItemsQueryHandler CreateHandler() =>
        new(_todoItems, _tenantContext, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new GetMyTodoItemsQueryHandler(_todoItems, _tenantContext, resolver);

        var result = await handler.Handle(new GetMyTodoItemsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Map_The_Owners_Items_To_Dtos()
    {
        var item = TodoItem.Create(TenantId, EmployeeId, "Submit timesheet", new DateOnly(2026, 2, 1), TodoUrgency.High, 0).Value;
        _todoItems.ListAsync(Arg.Any<TodoItemsByOwnerSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<TodoItem>)[item]);

        var result = await CreateHandler().Handle(new GetMyTodoItemsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto =>
            dto.Id == item.Id.Value && dto.Title == "Submit timesheet" && dto.Urgency == "High" && !dto.IsDone);
    }
}
