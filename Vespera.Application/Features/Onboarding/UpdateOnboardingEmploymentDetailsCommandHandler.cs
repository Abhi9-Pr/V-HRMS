using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class UpdateOnboardingEmploymentDetailsCommandHandler : IRequestHandler<UpdateOnboardingEmploymentDetailsCommand, Result>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateOnboardingEmploymentDetailsCommandHandler(
        IReadRepository<OnboardingDraft> drafts,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateOnboardingEmploymentDetailsCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        return draft.UpdateEmploymentDetails(
            new DepartmentId(request.DepartmentId), new DesignationId(request.DesignationId), new LocationId(request.LocationId),
            request.DateOfJoining, now, modifiedBy);
    }
}
