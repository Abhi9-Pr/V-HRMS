using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Application.Features.Helpdesk;

public sealed class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, Result<Guid>>
{
    private readonly IWriteRepository<SlaPolicy> _policies;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSlaPolicyCommandHandler(
        IWriteRepository<SlaPolicy> policies, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _policies = policies;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var result = SlaPolicy.Create(
            _tenantContext.TenantId, request.Name, TimeSpan.FromHours(request.ResponseTimeHours), TimeSpan.FromHours(request.ResolutionTimeHours),
            _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system", request.BusinessHoursStart, request.BusinessHoursEnd);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _policies.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
