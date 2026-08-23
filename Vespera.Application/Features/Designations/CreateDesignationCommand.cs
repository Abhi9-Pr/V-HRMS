using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

public sealed record CreateDesignationCommand(
    string Title,
    int Grade,
    string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
