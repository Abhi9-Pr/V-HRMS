using MediatR;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed class AnnouncementsWidgetProvider : IDashboardWidgetProvider
{
    private readonly ISender _sender;

    public AnnouncementsWidgetProvider(ISender sender)
    {
        _sender = sender;
    }

    public string WidgetKey => "announcements";

    public int DefaultOrder => 1;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Medium;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAnnouncementsForMeQuery(), cancellationToken);
        return result.IsFailure ? Result.Failure<object?>(result.Error) : Result.Success<object?>(result.Value);
    }
}
