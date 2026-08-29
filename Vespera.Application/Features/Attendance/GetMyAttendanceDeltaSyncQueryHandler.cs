using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.Features.Attendance;

/// <summary>Self-service delta sync — scopes to whichever <see cref="Domain.Eis.Employee"/> the
/// calling <see cref="Domain.IdentityAccess.User"/> is linked to, resolved the same way
/// <c>SubordinateOrSelfAuthorizationHandler</c> resolves "which employee is this caller." A caller
/// with no linked employee (e.g. a pure admin account) gets a well-formed, empty result rather than
/// a rejection — "no attendance to sync" is a legitimate answer for that caller, not an error.</summary>
public sealed class GetMyAttendanceDeltaSyncQueryHandler
    : DeltaSyncQueryHandlerBase<GetMyAttendanceDeltaSyncQuery, AttendanceDay, AttendanceDaySummaryDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IReadRepository<User> _users;

    public GetMyAttendanceDeltaSyncQueryHandler(
        IReadRepository<AttendanceDay> repository,
        IDateTimeProvider dateTimeProvider,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IReadRepository<User> users)
        : base(repository, dateTimeProvider)
    {
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _users = users;
    }

    protected override DeltaSyncRequest GetDeltaSyncRequest(GetMyAttendanceDeltaSyncQuery request) =>
        new(request.Since, request.Cursor, request.PageSize <= 0 ? 100 : request.PageSize);

    protected override async Task<ISpecification<AttendanceDay>> BuildSpecification(
        GetMyAttendanceDeltaSyncQuery request, DeltaSyncRequest deltaSync, CancellationToken cancellationToken)
    {
        var employeeId = await ResolveCallerEmployeeIdAsync(cancellationToken);
        return new AttendanceDaysByEmployeeSinceSpecification(_tenantContext.TenantId, employeeId);
    }

    protected override Guid GetId(AttendanceDay entity) => entity.Id.Value;

    protected override DateTimeOffset GetLastChanged(AttendanceDay entity) => entity.LastChangedAt;

    protected override bool IsTombstoned(AttendanceDay entity) => entity.IsDeleted;

    protected override AttendanceDaySummaryDto MapToDto(AttendanceDay entity) => new(
        entity.Id.Value, entity.Date, entity.Status.ToString(), entity.WorkedMinutes,
        entity.Punches.Any(p => p.RequiresApproval));

    private async Task<EmployeeId> ResolveCallerEmployeeIdAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userIdValue)
        {
            return EmployeeId.New();
        }

        var callerUser = await _users.FirstOrDefaultAsync(new UserByIdSpecification(new UserId(userIdValue)), cancellationToken);
        return callerUser?.EmployeeId ?? EmployeeId.New();
    }
}
