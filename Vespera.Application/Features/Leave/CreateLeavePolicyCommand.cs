using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record CreateLeavePolicyCommand(
    Guid LeaveTypeId, decimal AnnualEntitlementDays, decimal AccrualRatePerMonth, decimal MaxCarryForwardDays, DateOnly ValidFrom)
    : IRequest<Result<Guid>>;
