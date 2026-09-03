using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class TodoItemTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId OwnerId = EmployeeId.New();

    [Fact]
    public void Create_Should_Reject_Blank_Title()
    {
        var result = TodoItem.Create(TenantId, OwnerId, "   ", null, TodoUrgency.Medium, 0);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Complete_Then_Reopen_Should_Round_Trip()
    {
        var item = CreateItem();

        item.Complete().IsSuccess.Should().BeTrue();
        item.IsDone.Should().BeTrue();

        item.Reopen().IsSuccess.Should().BeTrue();
        item.IsDone.Should().BeFalse();
    }

    [Fact]
    public void Complete_Should_Fail_When_Already_Done()
    {
        var item = CreateItem();
        item.Complete();

        item.Complete().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reorder_Should_Update_SortOrder()
    {
        var item = CreateItem();

        item.Reorder(5);

        item.SortOrder.Should().Be(5);
    }

    [Fact]
    public void SetUrgency_Should_Update_Urgency()
    {
        var item = CreateItem();

        item.SetUrgency(TodoUrgency.High);

        item.Urgency.Should().Be(TodoUrgency.High);
    }

    [Fact]
    public void Reopen_Should_Fail_When_Not_Done()
    {
        var item = CreateItem();

        var result = item.Reopen();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("todo_item.not_done");
    }

    [Fact]
    public void Reschedule_Should_Update_DueDate()
    {
        var item = CreateItem();
        var dueDate = new DateOnly(2026, 3, 1);

        item.Reschedule(dueDate);

        item.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void Reschedule_Should_Allow_Clearing_The_DueDate()
    {
        var item = CreateItem();
        item.Reschedule(new DateOnly(2026, 3, 1));

        item.Reschedule(null);

        item.DueDate.Should().BeNull();
    }

    private static TodoItem CreateItem() =>
        TodoItem.Create(TenantId, OwnerId, "Submit timesheet", null, TodoUrgency.Medium, 0).Value;
}
