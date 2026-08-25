using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record CreateSoftwareLicenseCommand(
    string ProductName, int SeatCount, DateOnly? ExpiresAt, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
