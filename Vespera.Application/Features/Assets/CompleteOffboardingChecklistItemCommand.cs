using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record CompleteOffboardingChecklistItemCommand(
    Guid ChecklistId, int ItemIndex, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
