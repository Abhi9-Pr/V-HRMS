using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Result<Guid>>
{
    private readonly IReadRepository<TodoItem> _todoItems;
    private readonly IWriteRepository<TodoItem> _todoItemWriter;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public CreateTodoItemCommandHandler(
        IReadRepository<TodoItem> todoItems, IWriteRepository<TodoItem> todoItemWriter, ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _todoItems = todoItems;
        _todoItemWriter = todoItemWriter;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Failure<Guid>(Error.Validation("todo_item.no_employee", "The signed-in account is not linked to an employee."));
        }

        var tenantId = _tenantContext.TenantId;
        var existing = await _todoItems.ListAsync(new TodoItemsByOwnerSpecification(tenantId, employeeId.Value), cancellationToken);
        var nextSortOrder = existing.Count == 0 ? 0 : existing.Max(item => item.SortOrder) + 1;

        var result = TodoItem.Create(tenantId, employeeId.Value, request.Title, request.DueDate, request.Urgency, nextSortOrder);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _todoItemWriter.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
