using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed class ConfirmFinalSettlementCommandHandler : IRequestHandler<ConfirmFinalSettlementCommand, Result>
{
    private readonly IReadRepository<OffboardingChecklist> _checklists;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ConfirmFinalSettlementCommandHandler(
        IReadRepository<OffboardingChecklist> checklists, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _checklists = checklists;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ConfirmFinalSettlementCommand request, CancellationToken cancellationToken)
    {
        var specification = new OffboardingChecklistByEmployeeIdSpecification(_tenantContext.TenantId, new EmployeeId(request.EmployeeId));
        var checklist = await _checklists.FirstOrDefaultAsync(specification, cancellationToken);
        if (checklist is null)
        {
            return Result.Failure(Error.NotFound("offboarding_checklist.not_found", "No offboarding checklist exists for this employee."));
        }

        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";
        return checklist.MarkFinalSettlementProcessed(_dateTimeProvider.UtcNow, modifiedBy);
    }
}
