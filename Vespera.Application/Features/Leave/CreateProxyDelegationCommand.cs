using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>"Holiday Mode": the caller (the delegator) routes their pending and new approvals for
/// <paramref name="Scope"/> to <paramref name="DelegateEmployeeId"/> for the given window. The org
/// structure — reporting relationships, nominal approvers on any chain already built — is
/// unchanged; only who is *authorized to act* changes, resolved fresh on every decision by
/// <see cref="LeaveApprovalStepAuthorizer"/>.</summary>
public sealed record CreateProxyDelegationCommand(Guid DelegateEmployeeId, DateOnly From, DateOnly To, string Scope)
    : IRequest<Result<Guid>>;
