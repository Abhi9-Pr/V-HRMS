using MediatR;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Mobile;

/// <summary>Everything the app needs on first launch in one round trip — see
/// docs/api-mobile-contract.md and AGENTS.md's mobile-hardening brief. Self-service only, same as
/// every other <c>GetMy*</c> mobile query: always the caller's own employee record, never a
/// request parameter. Deliberately does not include granted permissions or feature flags — see
/// <c>MobileBootstrapController</c>'s doc comment for where/why those two are added instead.</summary>
public sealed record GetMobileBootstrapQuery : IRequest<Result<MobileBootstrapDto>>;

public sealed record EmployeeProfileDto(
    Guid EmployeeId,
    string Code,
    string FirstName,
    string LastName,
    string WorkEmail,
    Guid DepartmentId,
    Guid DesignationId,
    Guid LocationId,
    string Status);

public sealed record GeofenceZoneDto(Guid Id, string Name, double Latitude, double Longitude, double RadiusMetres);

/// <summary>Hash-based version stamps for each reference-data set the app caches offline — the
/// same RowVersion hash each set's own <c>Get*ETagQuery</c> computes (see
/// docs/api-mobile-contract.md's ETag convention). A client compares these against what it has
/// locally cached and only re-fetches (or delta-syncs) a set whose stamp changed, instead of
/// blindly re-downloading every reference list on every cold start.</summary>
public sealed record PolicyVersionsDto(string Departments, string Designations, string Locations, string LeaveTypes, string Holidays);

public sealed record MobileBootstrapDto(
    EmployeeProfileDto Profile,
    IReadOnlyList<RosterDayDto> UpcomingShifts,
    IReadOnlyList<GeofenceZoneDto> Geofences,
    PolicyVersionsDto PolicyVersions);
