using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

/// <summary><paramref name="ApplicableGender"/> null means open to every gender; a non-null value
/// must match a <see cref="Domain.Eis.Gender"/> name.</summary>
public sealed record UpdateLeaveTypeEligibilityCommand(
    Guid LeaveTypeId, string? ApplicableGender, int MinimumTenureMonths, bool IsEncashable, decimal MaxEncashableDays)
    : IRequest<Result>;
