using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Mobile;

/// <summary>Stops push delivery to one of the caller's own devices — called on sign-out so a
/// shared/reset device doesn't keep receiving another user's notifications.</summary>
public sealed record DeactivateDeviceCommand(Guid DeviceRegistrationId, string? IdempotencyKey = null)
    : IRequest<Result>, IIdempotentRequest;
