using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record RejectLeaveRequestCommand(Guid LeaveRequestId, string Reason, string? IdempotencyKey = null)
    : IRequest<Result>, IIdempotentRequest;
