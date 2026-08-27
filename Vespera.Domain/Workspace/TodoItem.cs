using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Workspace;

public readonly record struct TodoItemId(Guid Value)
{
    public static TodoItemId New() => new(Guid.NewGuid());
}

public enum TodoUrgency
{
    Low,
    Medium,
    High,
}

public sealed class TodoItem : AggregateRoot<TodoItemId>, ITenantScoped
{
    private TodoItem(
        TodoItemId id, TenantId tenantId, EmployeeId ownerEmployeeId, string title, DateOnly? dueDate, TodoUrgency urgency,
        int sortOrder)
        : base(id)
    {
        TenantId = tenantId;
        OwnerEmployeeId = ownerEmployeeId;
        Title = title;
        DueDate = dueDate;
        Urgency = urgency;
        SortOrder = sortOrder;
        IsDone = false;
    }

    public TenantId TenantId { get; }

    public EmployeeId OwnerEmployeeId { get; }

    public string Title { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public TodoUrgency Urgency { get; private set; }

    /// <summary>Owner-defined manual ordering within their own list — the drag-and-drop position,
    /// not a computed priority. Ties (e.g. two items created before ordering existed) fall back to
    /// creation order at the query layer.</summary>
    public int SortOrder { get; private set; }

    public bool IsDone { get; private set; }

    public static Result<TodoItem> Create(
        TenantId tenantId, EmployeeId ownerEmployeeId, string title, DateOnly? dueDate, TodoUrgency urgency, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<TodoItem>(Error.Validation("todo_item.title_required", "Title is required."));
        }

        return Result.Success(new TodoItem(TodoItemId.New(), tenantId, ownerEmployeeId, title.Trim(), dueDate, urgency, sortOrder));
    }

    public Result Reorder(int sortOrder)
    {
        SortOrder = sortOrder;
        return Result.Success();
    }

    public Result SetUrgency(TodoUrgency urgency)
    {
        Urgency = urgency;
        return Result.Success();
    }

    public Result Complete()
    {
        if (IsDone)
        {
            return Result.Failure(Error.Conflict("todo_item.already_done", "Item is already complete."));
        }

        IsDone = true;
        return Result.Success();
    }

    public Result Reopen()
    {
        if (!IsDone)
        {
            return Result.Failure(Error.Conflict("todo_item.not_done", "Item is not complete."));
        }

        IsDone = false;
        return Result.Success();
    }

    public Result Reschedule(DateOnly? dueDate)
    {
        DueDate = dueDate;
        return Result.Success();
    }
}
