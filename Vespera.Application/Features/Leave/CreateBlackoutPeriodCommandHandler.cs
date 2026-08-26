using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Leave;

public sealed class CreateBlackoutPeriodCommandHandler : IRequestHandler<CreateBlackoutPeriodCommand, Result<Guid>>
{
    private readonly IWriteRepository<BlackoutPeriod> _writer;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBlackoutPeriodCommandHandler(
        IWriteRepository<BlackoutPeriod> writer, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _writer = writer;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBlackoutPeriodCommand request, CancellationToken cancellationToken)
    {
        var periodResult = DateRange.Create(request.From, request.To);
        if (periodResult.IsFailure)
        {
            return Result.Failure<Guid>(periodResult.Error);
        }

        var createdBy = _currentUser.Email ?? "system";
        var result = BlackoutPeriod.Create(
            _tenantContext.TenantId, periodResult.Value, request.Reason,
            request.LeaveTypeId is { } id ? new LeaveTypeId(id) : null, _dateTimeProvider.UtcNow, createdBy);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _writer.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
