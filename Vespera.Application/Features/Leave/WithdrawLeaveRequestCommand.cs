using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>Withdraws the caller's own still-<c>Pending</c> request — distinct from
/// <see cref="CancelApprovedLeaveRequestCommand"/>, which handles an already-<c>Approved</c> one.</summary>
public sealed record WithdrawLeaveRequestCommand(Guid LeaveRequestId, string? IdempotencyKey = null)
    : IRequest<Result>, IIdempotentRequest;
