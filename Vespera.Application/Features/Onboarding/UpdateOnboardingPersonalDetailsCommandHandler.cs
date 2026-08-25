using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Onboarding;

public sealed class UpdateOnboardingPersonalDetailsCommandHandler : IRequestHandler<UpdateOnboardingPersonalDetailsCommand, Result>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateOnboardingPersonalDetailsCommandHandler(
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

    public async Task<Result> Handle(UpdateOnboardingPersonalDetailsCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var email = EmailAddress.Create(request.WorkEmail);
        if (email.IsFailure)
        {
            return Result.Failure(email.Error);
        }

        var phone = PhoneNumber.Create(request.Phone);
        if (phone.IsFailure)
        {
            return Result.Failure(phone.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";

        return draft.UpdatePersonalDetails(request.FirstName, request.LastName, email.Value, phone.Value, request.DateOfBirth, now, modifiedBy);
    }
}
