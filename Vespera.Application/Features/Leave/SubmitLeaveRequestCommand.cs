using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary><paramref name="AcknowledgeInsufficientBalance"/> must be true when the request would
/// consume more days than are available under a policy whose <see cref="Domain.Leave.NegativeBalancePolicy"/>
/// is <see cref="Domain.Leave.NegativeBalancePolicy.AllowWithLop"/> — the handler reports back how
/// many days would go to loss-of-pay without it, so the client can show that number and resubmit
/// with the flag set.</summary>
public sealed record SubmitLeaveRequestCommand(
    Guid LeaveTypeId, DateOnly From, DateOnly To, string Reason, bool AcknowledgeInsufficientBalance)
    : IRequest<Result<SubmitLeaveRequestResponse>>;

public sealed record SubmitLeaveRequestResponse(Guid LeaveRequestId, decimal RequestedDays, decimal LossOfPayDays);
