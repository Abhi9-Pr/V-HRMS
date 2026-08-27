using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

/// <summary>Whether one employee falls inside one announcement's audience — the same rule used
/// both to filter "announcements for me" and to build the HR read-receipts report's target
/// roster, kept in one place so the two can't drift apart.</summary>
public static class AnnouncementAudienceMatcher
{
    public static bool Matches(Announcement announcement, Employee employee) => announcement.AudienceScope switch
    {
        AnnouncementAudienceScope.AllEmployees => true,
        AnnouncementAudienceScope.Department => announcement.TargetDepartmentId == employee.DepartmentId,
        AnnouncementAudienceScope.Location => announcement.TargetLocationId == employee.LocationId,
        _ => false,
    };
}
