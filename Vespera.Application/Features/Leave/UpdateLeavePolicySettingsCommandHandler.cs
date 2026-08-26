using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class UpdateLeavePolicySettingsCommandHandler : IRequestHandler<UpdateLeavePolicySettingsCommand, Result>
{
    private readonly IReadRepository<LeavePolicy> _policies;
    private readonly IWriteRepository<LeavePolicy> _writer;
    private readonly ITenantContext _tenantContext;

    public UpdateLeavePolicySettingsCommandHandler(
        IReadRepository<LeavePolicy> policies, IWriteRepository<LeavePolicy> writer, ITenantContext tenantContext)
    {
        _policies = policies;
        _writer = writer;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(UpdateLeavePolicySettingsCommand request, CancellationToken cancellationToken)
    {
        var policy = await _policies.FirstOrDefaultAsync(
            new LeavePolicyByIdSpecification(_tenantContext.TenantId, new LeavePolicyId(request.LeavePolicyId)), cancellationToken);
        if (policy is null)
        {
            return Result.Failure(Error.NotFound("leave_policy.not_found", "Leave policy not found."));
        }

        var accrualResult = policy.ConfigureAccrual(
            Enum.Parse<AccrualFrequency>(request.AccrualFrequency), request.MinimumTenureMonthsForAccrual, policy.TenureAccrualTiers);
        if (accrualResult.IsFailure)
        {
            return accrualResult;
        }

        var chainResult = policy.ConfigureApprovalChain(
            request.RequiresSkipLevelApproval, request.SkipLevelThresholdDays, request.RequiresHrApproval);
        if (chainResult.IsFailure)
        {
            return chainResult;
        }

        var balanceResult = policy.ConfigureBalanceRules(
            Enum.Parse<NegativeBalancePolicy>(request.NegativeBalancePolicy), request.MaxNegativeBalanceDays, request.SandwichLeaveEnabled);
        if (balanceResult.IsFailure)
        {
            return balanceResult;
        }

        _writer.Update(policy);
        return Result.Success();
    }
}
