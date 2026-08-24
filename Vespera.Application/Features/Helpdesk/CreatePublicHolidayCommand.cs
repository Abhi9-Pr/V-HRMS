using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record CreatePublicHolidayCommand(DateOnly Date, string Name, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
