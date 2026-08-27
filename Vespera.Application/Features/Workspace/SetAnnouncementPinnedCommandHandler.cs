using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed class SetAnnouncementPinnedCommandHandler : IRequestHandler<SetAnnouncementPinnedCommand, Result>
{
    private readonly IReadRepository<Announcement> _announcements;
    private readonly IWriteRepository<Announcement> _announcementWriter;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetAnnouncementPinnedCommandHandler(
        IReadRepository<Announcement> announcements, IWriteRepository<Announcement> announcementWriter, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _announcements = announcements;
        _announcementWriter = announcementWriter;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetAnnouncementPinnedCommand request, CancellationToken cancellationToken)
    {
        var announcement = await _announcements.FirstOrDefaultAsync(
            new AnnouncementByIdSpecification(_tenantContext.TenantId, new AnnouncementId(request.AnnouncementId)), cancellationToken);
        if (announcement is null)
        {
            return Result.Failure(Error.NotFound("announcement.not_found", "Announcement not found."));
        }

        var modifiedBy = _currentUser.UserId?.ToString() ?? "system";
        var now = _dateTimeProvider.UtcNow;
        var result = request.Pinned ? announcement.Pin(now, modifiedBy) : announcement.Unpin(now, modifiedBy);
        if (result.IsFailure)
        {
            return result;
        }

        _announcementWriter.Update(announcement);
        return Result.Success();
    }
}
