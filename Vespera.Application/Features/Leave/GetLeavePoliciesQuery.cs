using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record GetLeavePoliciesQuery : IRequest<Result<IReadOnlyList<LeavePolicyDto>>>;

public sealed record LeavePolicyDto(
    Guid Id,
    Guid LeaveTypeId,
    decimal AnnualEntitlementDays,
    decimal AccrualRatePerMonth,
    decimal MaxCarryForwardDays,
    string AccrualFrequency,
    bool RequiresSkipLevelApproval,
    decimal? SkipLevelThresholdDays,
    bool RequiresHrApproval,
    string NegativeBalancePolicy,
    decimal MaxNegativeBalanceDays,
    bool SandwichLeaveEnabled,
    DateOnly ValidFrom,
    DateOnly? ValidTo);
