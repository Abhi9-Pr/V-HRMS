using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class CreateAnnouncementCommandHandler : IRequestHandler<CreateAnnouncementCommand, Result<Guid>>
{
    private readonly IWriteRepository<Announcement> _announcements;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAnnouncementCommandHandler(
        IWriteRepository<Announcement> announcements, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _announcements = announcements;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var createdBy = _currentUser.UserId?.ToString() ?? "system";

        var result = Announcement.Create(
            _tenantContext.TenantId, request.Title, request.Body, request.AudienceScope,
            request.TargetDepartmentId is { } departmentId ? new DepartmentId(departmentId) : null,
            request.TargetLocationId is { } locationId ? new LocationId(locationId) : null,
            request.Priority, request.PublishAt, request.ExpiresAt, now, createdBy);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var announcement = result.Value;

        if (request.PublishImmediately)
        {
            var publishResult = announcement.Publish(now, createdBy);
            if (publishResult.IsFailure)
            {
                return Result.Failure<Guid>(publishResult.Error);
            }
        }

        await _announcements.AddAsync(announcement, cancellationToken);

        return Result.Success(announcement.Id.Value);
    }
}
