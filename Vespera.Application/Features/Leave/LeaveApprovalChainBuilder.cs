using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Builds the nominal approver sequence for a newly-submitted <see cref="LeaveRequest"/>:
/// direct manager (tier 1, mandatory), the manager's own manager (tier 2, when
/// <see cref="LeavePolicy.RequiresSkipLevelApproval"/> or the request exceeds
/// <see cref="LeavePolicy.SkipLevelThresholdDays"/>), and a final HR sign-off (when
/// <see cref="LeavePolicy.RequiresHrApproval"/>) resolved to the tenant's first active user in the
/// "HR" role — a single designated approver, not a queue; a future phase can round-robin across HR
/// without changing anything downstream, since <see cref="ApprovalChain"/> only ever sees one
/// <see cref="EmployeeId"/> per tier. These are the *nominal* approvers — <see cref="ApprovalStep.ApproverId"/>;
/// who is actually authorized to decide a given step is re-resolved per decision by
/// <see cref="LeaveApprovalStepAuthorizer"/>, since a proxy delegation can change after the chain is
/// built.</summary>
public sealed class LeaveApprovalChainBuilder
{
    private const string HrRoleName = "HR";

    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IReadRepository<Role> _roles;
    private readonly IReadRepository<User> _users;

    public LeaveApprovalChainBuilder(
        IReadRepository<ReportingRelationship> reportingRelationships, IReadRepository<Role> roles, IReadRepository<User> users)
    {
        _reportingRelationships = reportingRelationships;
        _roles = roles;
        _users = users;
    }

    public async Task<Result<IReadOnlyList<EmployeeId>>> BuildApproverSequenceAsync(
        TenantId tenantId, EmployeeId requestingEmployeeId, LeavePolicy policy, decimal requestedDays, DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var managerRelationship = await _reportingRelationships.FirstOrDefaultAsync(
            new ActiveManagerRelationshipSpecification(requestingEmployeeId, asOf), cancellationToken);
        if (managerRelationship is null)
        {
            return Result.Failure<IReadOnlyList<EmployeeId>>(
                Error.Validation("leave_request.no_manager", "No active manager is configured for this employee."));
        }

        var sequence = new List<EmployeeId> { managerRelationship.ManagerId };

        var needsSkipLevel = policy.RequiresSkipLevelApproval
            || (policy.SkipLevelThresholdDays is { } threshold && requestedDays > threshold);
        if (needsSkipLevel)
        {
            var skipLevelRelationship = await _reportingRelationships.FirstOrDefaultAsync(
                new ActiveManagerRelationshipSpecification(managerRelationship.ManagerId, asOf), cancellationToken);
            if (skipLevelRelationship is not null && !sequence.Contains(skipLevelRelationship.ManagerId))
            {
                sequence.Add(skipLevelRelationship.ManagerId);
            }
        }

        if (policy.RequiresHrApproval)
        {
            var hrApproverId = await ResolveHrApproverAsync(tenantId, cancellationToken);
            if (hrApproverId is null)
            {
                return Result.Failure<IReadOnlyList<EmployeeId>>(Error.Validation(
                    "leave_request.no_hr_approver", "This leave type requires HR approval, but no HR user is configured for this tenant."));
            }

            if (!sequence.Contains(hrApproverId.Value))
            {
                sequence.Add(hrApproverId.Value);
            }
        }

        return Result.Success<IReadOnlyList<EmployeeId>>(sequence);
    }

    private async Task<EmployeeId?> ResolveHrApproverAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        var hrRole = await _roles.FirstOrDefaultAsync(new RoleByTenantAndNameSpecification(tenantId, HrRoleName), cancellationToken);
        if (hrRole is null)
        {
            return null;
        }

        var users = await _users.ListAsync(new UsersByTenantSpecification(tenantId), cancellationToken);
        var hrUser = users
            .Where(u => u.EmployeeId is not null && u.RoleIds.Contains(hrRole.Id))
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefault();

        return hrUser?.EmployeeId;
    }
}
