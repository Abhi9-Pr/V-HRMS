using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary>Covers all three of <see cref="Domain.Leave.LeavePolicy"/>'s configuration surfaces
/// (accrual, approval chain, balance rules) in one round trip — an HR settings screen edits them
/// together, not one field at a time.</summary>
public sealed record UpdateLeavePolicySettingsCommand(
    Guid LeavePolicyId,
    string AccrualFrequency,
    int MinimumTenureMonthsForAccrual,
    bool RequiresSkipLevelApproval,
    decimal? SkipLevelThresholdDays,
    bool RequiresHrApproval,
    string NegativeBalancePolicy,
    decimal MaxNegativeBalanceDays,
    bool SandwichLeaveEnabled)
    : IRequest<Result>;
