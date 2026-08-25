using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>Resolves who is actually authorized to decide a regularization request for a given
/// employee — the requesting employee's manager (via the same reporting-relationship lookup
/// <c>SubordinateOrSelfAuthorizationHandler</c> uses, <see cref="ActiveManagerRelationshipSpecification"/>),
/// then run through <see cref="ApprovalChainResolver"/> against that manager's active
/// <see cref="ProxyDelegation"/>s — the delegate decides, not the nominal manager, when one is
/// active. Shared by Approve/Reject/the team inbox so the rule lives in exactly one place.</summary>
public sealed class RegularizationApproverResolver
{
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IReadRepository<ProxyDelegation> _delegations;
    private readonly ApprovalChainResolver _approvalChainResolver = new();

    public RegularizationApproverResolver(
        IReadRepository<ReportingRelationship> reportingRelationships, IReadRepository<ProxyDelegation> delegations)
    {
        _reportingRelationships = reportingRelationships;
        _delegations = delegations;
    }

    public async Task<EmployeeId?> ResolveAuthorizedApproverAsync(
        EmployeeId requestingEmployeeId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var managerRelationship = await _reportingRelationships.FirstOrDefaultAsync(
            new ActiveManagerRelationshipSpecification(requestingEmployeeId, asOf), cancellationToken);
        if (managerRelationship is null)
        {
            return null;
        }

        var nominalApproverId = managerRelationship.ManagerId;

        var delegations = (await _delegations.ListAsync(
                new DelegationsByDelegatorSpecification(nominalApproverId), cancellationToken))
            .Where(d => d.IsActiveOn(asOf) && d.Scope is DelegationScope.AttendanceApprovals or DelegationScope.All)
            .ToList();

        return _approvalChainResolver.ResolveApprover(nominalApproverId, asOf, delegations);
    }
}
