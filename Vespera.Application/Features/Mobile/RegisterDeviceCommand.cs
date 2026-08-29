using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Mobile;

/// <summary>Registers (or, for a device the caller already registered, re-registers — see
/// <see cref="RegisterDeviceCommandHandler"/>'s upsert-by-device-id behavior) one physical device
/// for push. Called on app launch and whenever the platform hands the app a fresh push token.</summary>
public sealed record RegisterDeviceCommand(
    string DeviceId, DevicePlatform Platform, string PushToken, string? IdempotencyKey = null)
    : IRequest<Result<Guid>>, IIdempotentRequest;
