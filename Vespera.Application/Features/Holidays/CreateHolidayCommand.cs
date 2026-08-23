using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed record CreateHolidayCommand(
    Guid LocationId,
    DateOnly Date,
    string Name,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
