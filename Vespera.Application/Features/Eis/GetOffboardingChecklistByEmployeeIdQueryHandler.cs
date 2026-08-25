using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class GetOffboardingChecklistByEmployeeIdQueryHandler
    : IRequestHandler<GetOffboardingChecklistByEmployeeIdQuery, Result<OffboardingChecklistDto>>
{
    private readonly IReadRepository<OffboardingChecklist> _checklists;
    private readonly ITenantContext _tenantContext;

    public GetOffboardingChecklistByEmployeeIdQueryHandler(IReadRepository<OffboardingChecklist> checklists, ITenantContext tenantContext)
    {
        _checklists = checklists;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OffboardingChecklistDto>> Handle(GetOffboardingChecklistByEmployeeIdQuery request, CancellationToken cancellationToken)
    {
        var specification = new OffboardingChecklistByEmployeeIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var checklist = await _checklists.FirstOrDefaultAsync(specification, cancellationToken);
        if (checklist is null)
        {
            return Result.Failure<OffboardingChecklistDto>(
                Error.NotFound("offboarding_checklist.not_found", "No offboarding checklist exists for this employee."));
        }

        var dto = new OffboardingChecklistDto(
            checklist.Id.Value,
            checklist.EmployeeId.Value,
            checklist.ExitDate,
            checklist.AccessRevokedStatus.ToString(),
            checklist.AccessRevokedAt,
            checklist.AssetsRecoveredStatus.ToString(),
            checklist.AssetsRecoveredAt,
            checklist.FinalSettlementStatus.ToString(),
            checklist.FinalSettlementAt);

        return Result.Success(dto);
    }
}
