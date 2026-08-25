using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Onboarding;

public sealed class SubmitOnboardingCommandHandler : IRequestHandler<SubmitOnboardingCommand, Result<Guid>>
{
    private readonly IReadRepository<OnboardingDraft> _drafts;
    private readonly IWriteRepository<Employee> _employees;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SubmitOnboardingCommandHandler(
        IReadRepository<OnboardingDraft> drafts,
        IWriteRepository<Employee> employees,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _drafts = drafts;
        _employees = employees;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(SubmitOnboardingCommand request, CancellationToken cancellationToken)
    {
        var specification = new OnboardingDraftByIdSpecification(_tenantContext.TenantId, new OnboardingDraftId(request.OnboardingDraftId));
        var draft = await _drafts.FirstOrDefaultAsync(specification, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<Guid>(Error.NotFound("onboarding_draft.not_found", "Onboarding draft not found."));
        }

        var code = EmployeeCode.Create(request.EmployeeCode);
        if (code.IsFailure)
        {
            return Result.Failure<Guid>(code.Error);
        }

        var now = _dateTimeProvider.UtcNow;
        var submittedBy = _currentUser.UserId?.ToString() ?? "system";

        var submitResult = draft.Submit(now, submittedBy);
        if (submitResult.IsFailure)
        {
            return Result.Failure<Guid>(submitResult.Error);
        }

        var convertResult = draft.ConvertToEmployee(code.Value, now, submittedBy);
        if (convertResult.IsFailure)
        {
            return Result.Failure<Guid>(convertResult.Error);
        }

        await _employees.AddAsync(convertResult.Value, cancellationToken);
        return Result.Success(convertResult.Value.Id.Value);
    }
}
