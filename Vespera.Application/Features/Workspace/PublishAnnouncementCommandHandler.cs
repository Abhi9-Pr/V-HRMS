using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class PublishAnnouncementCommandHandler : IRequestHandler<PublishAnnouncementCommand, Result>
{
    private readonly IReadRepository<Announcement> _announcements;
    private readonly IWriteRepository<Announcement> _announcementWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishAnnouncementCommandHandler(
        IReadRepository<Announcement> announcements, IWriteRepository<Announcement> announcementWriter, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _announcements = announcements;
        _announcementWriter = announcementWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _announcements.FirstOrDefaultAsync(
            new AnnouncementByIdSpecification(_tenantContext.TenantId, new AnnouncementId(request.AnnouncementId)), cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(Error.NotFound("announcement.not_found", "Announcement not found."));
        }

        var result = announcement.Publish(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
        if (result.IsFailure)
        {
            return result;
        }

        _announcementWriter.Update(announcement);
        return Result.Success();
    }
}
