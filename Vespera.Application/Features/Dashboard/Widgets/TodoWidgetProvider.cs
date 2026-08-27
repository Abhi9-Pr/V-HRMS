using MediatR;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class TodoWidgetProvider : IDashboardWidgetProvider
{
    private readonly ISender _sender;

    public TodoWidgetProvider(ISender sender)
    {
        _sender = sender;
    }

    public string WidgetKey => "todos";

    public int DefaultOrder => 4;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Small;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMyTodoItemsQuery(), cancellationToken);
        return result.IsFailure ? Result.Failure<object?>(result.Error) : Result.Success<object?>(result.Value);
    }
}
