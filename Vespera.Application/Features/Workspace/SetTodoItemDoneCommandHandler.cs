using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class SetTodoItemDoneCommandHandler : IRequestHandler<SetTodoItemDoneCommand, Result>
{
    private readonly IReadRepository<TodoItem> _todoItems;
    private readonly IWriteRepository<TodoItem> _todoItemWriter;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public SetTodoItemDoneCommandHandler(
        IReadRepository<TodoItem> todoItems, IWriteRepository<TodoItem> todoItemWriter, ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _todoItems = todoItems;
        _todoItemWriter = todoItemWriter;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(SetTodoItemDoneCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure(Error.Validation("todo_item.no_employee", "The signed-in account is not linked to an employee."));
        }

        var todoItem = await _todoItems.FirstOrDefaultAsync(
            new TodoItemByIdSpecification(_tenantContext.TenantId, employeeId.Value, new TodoItemId(request.TodoItemId)), cancellationToken);
        if (todoItem is null)
        {
            return Result.Failure(Error.NotFound("todo_item.not_found", "To-do item not found."));
        }

        var result = request.Done ? todoItem.Complete() : todoItem.Reopen();
        if (result.IsFailure)
        {
            return result;
        }

        _todoItemWriter.Update(todoItem);
        return Result.Success();
    }
}
