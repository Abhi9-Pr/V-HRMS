using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Dashboard;

public sealed record GetDashboardQuery : IRequest<Result<DashboardResponseDto>>;

/// <summary>One visible widget's fetched payload. A failed/timed-out widget still gets an
/// envelope (<see cref="Success"/> false, <see cref="Data"/> null) rather than being omitted —
/// that's what lets one broken widget avoid blanking the rest of the dashboard.</summary>
public sealed record DashboardWidgetEnvelopeDto(string WidgetKey, int SortOrder, string Size, bool Success, object? Data, string? ErrorMessage);

/// <summary>The caller's full arrangement — every registered widget, visible or not — used to
/// drive the reorder/show-hide/resize UI. Data-free by design: a hidden widget's payload is never
/// fetched, which is part of how the aggregate call stays fast.</summary>
public sealed record DashboardWidgetPreferenceDto(string WidgetKey, int SortOrder, bool IsVisible, string Size);

public sealed record DashboardResponseDto(
    IReadOnlyList<DashboardWidgetPreferenceDto> Layout, IReadOnlyList<DashboardWidgetEnvelopeDto> Widgets);
