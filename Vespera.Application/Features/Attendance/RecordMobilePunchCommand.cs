using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

/// <summary>
/// <paramref name="RejectMockProvider"/>/<paramref name="MaxPlausibleSpeedKmh"/>/
/// <paramref name="MinAcceptableAccuracyMetres"/> are <c>MobilePunchOptions</c> resolved by the
/// Api-layer controller (the same config-at-the-edge pattern <c>WebPunchOptions</c>'s allowlist
/// check already uses) and threaded through here — Application has no existing
/// <c>IOptions&lt;T&gt;</c> precedent anywhere, and adding one just for three thresholds a
/// controller can resolve just as easily isn't worth a new cross-cutting pattern.
/// </summary>
public sealed record RecordMobilePunchCommand(
    Guid EmployeeId,
    string PunchType,
    double? Latitude,
    double? Longitude,
    double Accuracy,
    bool IsFromMockProvider,
    string DeviceId,
    bool RejectMockProvider,
    double MaxPlausibleSpeedKmh,
    double MinAcceptableAccuracyMetres,
    string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
