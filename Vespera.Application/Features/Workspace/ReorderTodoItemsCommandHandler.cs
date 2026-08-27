using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class ReorderTodoItemsCommandHandler : IRequestHandler<ReorderTodoItemsCommand, Result>
{
    private readonly IReadRepository<TodoItem> _todoItems;
    private readonly IWriteRepository<TodoItem> _todoItemWriter;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public ReorderTodoItemsCommandHandler(
        IReadRepository<TodoItem> todoItems, IWriteRepository<TodoItem> todoItemWriter, ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _todoItems = todoItems;
        _todoItemWriter = todoItemWriter;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(ReorderTodoItemsCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure(Error.Validation("todo_item.no_employee", "The signed-in account is not linked to an employee."));
        }

        var tenantId = _tenantContext.TenantId;
        var items = await _todoItems.ListAsync(new TodoItemsByOwnerSpecification(tenantId, employeeId.Value), cancellationToken);
        var itemsById = items.ToDictionary(item => item.Id.Value);

        if (request.OrderedTodoItemIds.Count != items.Count || request.OrderedTodoItemIds.Any(id => !itemsById.ContainsKey(id)))
        {
            return Result.Failure(Error.Validation(
                "todo_item.reorder_mismatch", "The reordered list must contain exactly this owner's current to-do items."));
        }

        for (var index = 0; index < request.OrderedTodoItemIds.Count; index++)
        {
            var item = itemsById[request.OrderedTodoItemIds[index]];
            item.Reorder(index);
            _todoItemWriter.Update(item);
        }

        return Result.Success();
    }
}
