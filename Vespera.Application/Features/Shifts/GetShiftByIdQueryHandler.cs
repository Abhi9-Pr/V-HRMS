using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed class GetShiftByIdQueryHandler : IRequestHandler<GetShiftByIdQuery, Result<ShiftDto>>
{
    private readonly IReadRepository<Shift> _shifts;
    private readonly ITenantContext _tenantContext;

    public GetShiftByIdQueryHandler(IReadRepository<Shift> shifts, ITenantContext tenantContext)
    {
        _shifts = shifts;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ShiftDto>> Handle(GetShiftByIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new ShiftByIdSpecification(_tenantContext.TenantId, new ShiftId(request.Id));

        var shift = await _shifts.FirstOrDefaultAsync(specification, cancellationToken);
        if (shift is null)
        {
            return Result.Failure<ShiftDto>(Error.NotFound("shift.not_found", "Shift not found."));
        }

        return Result.Success(shift.Adapt<ShiftDto>());
    }
}
