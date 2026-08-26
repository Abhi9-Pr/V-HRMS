using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

public sealed class GetLeaveTypesQueryHandler : IRequestHandler<GetLeaveTypesQuery, Result<IReadOnlyList<LeaveTypeDto>>>
{
    private readonly IReadRepository<LeaveType> _leaveTypes;
    private readonly ITenantContext _tenantContext;

    public GetLeaveTypesQueryHandler(IReadRepository<LeaveType> leaveTypes, ITenantContext tenantContext)
    {
        _leaveTypes = leaveTypes;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyList<LeaveTypeDto>>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        var leaveTypes = await _leaveTypes.ListAsync(new LeaveTypesByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var dtos = leaveTypes.Select(t => new LeaveTypeDto(
            t.Id.Value, t.Name, t.IsPaid, t.CarryForwardLimit, t.ApplicableGender?.ToString(), t.MinimumTenureMonths, t.IsEncashable,
            t.MaxEncashableDays)).ToList();

        return Result.Success<IReadOnlyList<LeaveTypeDto>>(dtos);
    }
}
