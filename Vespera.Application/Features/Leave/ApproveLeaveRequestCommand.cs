using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record ApproveLeaveRequestCommand(Guid LeaveRequestId, string? Comment, string? IdempotencyKey = null)
    : IRequest<Result>, IIdempotentRequest;
