using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class CreateRotationPatternCommandHandler : IRequestHandler<CreateRotationPatternCommand, Result<Guid>>
{
    private readonly IWriteRepository<RotationPattern> _rotationPatterns;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRotationPatternCommandHandler(
        IWriteRepository<RotationPattern> rotationPatterns,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _rotationPatterns = rotationPatterns;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateRotationPatternCommand request, CancellationToken cancellationToken)
    {
        var days = request.Days
            .Select(d => new RotationPatternDay(d.SequenceNumber, d.ShiftId is { } shiftId ? new ShiftId(shiftId) : null))
            .ToList();

        var result = RotationPattern.Create(
            _tenantContext.TenantId, request.Name, days, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _rotationPatterns.AddAsync(result.Value, cancellationToken);

        return Result.Success(result.Value.Id.Value);
    }
}
