using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;

namespace Vespera.Application.Features.Assets;

public sealed class GetOffboardingChecklistForEmployeeQueryHandler
    : IRequestHandler<GetOffboardingChecklistForEmployeeQuery, Result<OffboardingChecklistDto>>
{
    private readonly IReadRepository<OffboardingChecklist> _checklists;
    private readonly ITenantContext _tenantContext;

    public GetOffboardingChecklistForEmployeeQueryHandler(
        IReadRepository<OffboardingChecklist> checklists, ITenantContext tenantContext)
    {
        _checklists = checklists;
        _tenantContext = tenantContext;
    }

    public async Task<Result<OffboardingChecklistDto>> Handle(
        GetOffboardingChecklistForEmployeeQuery request, CancellationToken cancellationToken)
    {
        var checklist = await _checklists.FirstOrDefaultAsync(
            new OffboardingChecklistByEmployeeSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId)), cancellationToken);

        if (checklist is null)
        {
            return Result.Failure<OffboardingChecklistDto>(
                Error.NotFound("offboarding_checklist.not_found", "No offboarding checklist exists for this employee."));
        }

        var dto = new OffboardingChecklistDto(
            checklist.Id.Value,
            checklist.EmployeeId.Value,
            checklist.IsComplete,
            checklist.Items.Select(item => new OffboardingChecklistItemDto(item.Description, item.IsComplete)).ToList());

        return Result.Success(dto);
    }
}
