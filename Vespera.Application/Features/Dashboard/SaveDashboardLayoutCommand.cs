using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Dashboard;

public sealed record WidgetPreferenceInput(string WidgetKey, int SortOrder, bool IsVisible, string Size);

public sealed record SaveDashboardLayoutCommand(
    IReadOnlyList<WidgetPreferenceInput> Widgets, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
