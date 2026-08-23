using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>Cancels the caller's own already-<c>Approved</c> request, provided it hasn't started
/// yet — this is the "cancel-after-approval" flow whose ledger reversal, together with
/// <see cref="SubmitLeaveRequestCommand"/>'s original debit, is what the approve-then-cancel
/// reconciliation depends on.</summary>
public sealed record CancelApprovedLeaveRequestCommand(Guid LeaveRequestId, string Reason) : IRequest<Result>;
