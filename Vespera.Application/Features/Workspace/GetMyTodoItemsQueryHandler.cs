using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed class GetMyTodoItemsQueryHandler : IRequestHandler<GetMyTodoItemsQuery, Result<IReadOnlyList<TodoItemDto>>>
{
    private readonly IReadRepository<Domain.Workspace.TodoItem> _todoItems;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetMyTodoItemsQueryHandler(
        IReadRepository<Domain.Workspace.TodoItem> todoItems, ITenantContext tenantContext, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _todoItems = todoItems;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<IReadOnlyList<TodoItemDto>>> Handle(GetMyTodoItemsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<IReadOnlyList<TodoItemDto>>([]);
        }

        var items = await _todoItems.ListAsync(
            new TodoItemsByOwnerSpecification(_tenantContext.TenantId, employeeId.Value), cancellationToken);

        var dtos = items
            .Select(item => new TodoItemDto(item.Id.Value, item.Title, item.DueDate, item.Urgency.ToString(), item.SortOrder, item.IsDone))
            .ToList();

        return Result.Success<IReadOnlyList<TodoItemDto>>(dtos);
    }
}
