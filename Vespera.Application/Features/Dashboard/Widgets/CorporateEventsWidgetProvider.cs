using MediatR;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class CorporateEventsWidgetProvider : IDashboardWidgetProvider
{
    private readonly ISender _sender;

    public CorporateEventsWidgetProvider(ISender sender)
    {
        _sender = sender;
    }

    public string WidgetKey => "corporateEvents";

    public int DefaultOrder => 3;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Medium;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUpcomingCorporateEventsQuery(), cancellationToken);
        return result.IsFailure ? Result.Failure<object?>(result.Error) : Result.Success<object?>(result.Value);
    }
}
