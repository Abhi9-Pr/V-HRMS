using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed class DeleteRotationPatternCommandHandler : IRequestHandler<DeleteRotationPatternCommand, Result>
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteRotationPatternCommandHandler(
        IReadRepository<RotationPattern> rotationPatterns,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _rotationPatterns = rotationPatterns;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteRotationPatternCommand request, CancellationToken cancellationToken)
    {
        var specification = new RotationPatternByIdSpecification(_tenantContext.TenantId, new RotationPatternId(request.Id));

        var rotationPattern = await _rotationPatterns.FirstOrDefaultAsync(specification, cancellationToken);
        if (rotationPattern is null)
        {
            return Result.Failure(Error.NotFound("rotation_pattern.not_found", "Rotation pattern not found."));
        }

        return rotationPattern.Delete(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
