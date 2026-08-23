using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetLeavePoliciesQueryHandler : IRequestHandler<GetLeavePoliciesQuery, Result<IReadOnlyList<LeavePolicyDto>>>
{
    private readonly IReadRepository<LeavePolicy> _policies;
    private readonly ITenantContext _tenantContext;

    public GetLeavePoliciesQueryHandler(IReadRepository<LeavePolicy> policies, ITenantContext tenantContext)
    {
        _policies = policies;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<LeavePolicyDto>>> Handle(GetLeavePoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await _policies.ListAsync(new LeavePoliciesByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var dtos = policies.Select(p => new LeavePolicyDto(
            p.Id.Value, p.LeaveTypeId.Value, p.AnnualEntitlementDays, p.AccrualRatePerMonth, p.MaxCarryForwardDays,
            p.AccrualFrequency.ToString(), p.RequiresSkipLevelApproval, p.SkipLevelThresholdDays, p.RequiresHrApproval,
            p.NegativeBalancePolicy.ToString(), p.MaxNegativeBalanceDays, p.SandwichLeaveEnabled, p.ValidFrom, p.ValidTo)).ToList();

        return Result.Success<IReadOnlyList<LeavePolicyDto>>(dtos);
    }
}
