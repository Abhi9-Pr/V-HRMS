using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record RegisterBiometricDeviceCommand(
    Guid LocationId,
    string VendorType,
    string Host,
    int Port,
    string? ApiKeyConfigurationKey,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
