using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Onboarding;

public sealed class StartOnboardingDraftCommandHandler : IRequestHandler<StartOnboardingDraftCommand, Result<Guid>>
{
    private readonly IWriteRepository<OnboardingDraft> _drafts;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public StartOnboardingDraftCommandHandler(
        IWriteRepository<OnboardingDraft> drafts,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _drafts = drafts;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(StartOnboardingDraftCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var result = OnboardingDraft.StartDraft(_tenantContext.TenantId, now, createdBy);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _drafts.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
