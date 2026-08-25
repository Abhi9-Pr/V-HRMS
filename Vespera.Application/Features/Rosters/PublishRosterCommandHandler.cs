using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Rosters;

public sealed class PublishRosterCommandHandler : IRequestHandler<PublishRosterCommand, Result<int>>
{
    private readonly IReadRepository<ShiftRoster> _rosterReader;
    private readonly IWriteRepository<ShiftRoster> _rosterWriter;
    private readonly IReadRepository<Domain.IdentityAccess.User> _users;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PublishRosterCommandHandler(
        IReadRepository<ShiftRoster> rosterReader,
        IWriteRepository<ShiftRoster> rosterWriter,
        IReadRepository<Domain.IdentityAccess.User> users,
        INotificationDispatcher notificationDispatcher,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _rosterReader = rosterReader;
        _rosterWriter = rosterWriter;
        _users = users;
        _notificationDispatcher = notificationDispatcher;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<int>> Handle(PublishRosterCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;
        var now = _dateTimeProvider.UtcNow;
        var publishedBy = _currentUser.UserId?.ToString() ?? "system";

        var range = DateRange.Create(request.RangeStart, request.RangeEnd).Value;

        var draftRows = (await _rosterReader.ListAsync(
                new RostersByEmployeesAndStatusSpecification(tenantId, request.EmployeeIds, ShiftRosterStatus.Draft), cancellationToken))
            .Where(roster => roster.Period.Overlaps(range));

        var publishedCount = 0;
        var affectedEmployeeIds = new HashSet<EmployeeId>();

        foreach (var roster in draftRows)
        {
            var publishResult = roster.Publish(now, publishedBy);
            if (publishResult.IsFailure)
            {
                continue;
            }

            _rosterWriter.Update(roster);
            publishedCount++;
            affectedEmployeeIds.Add(roster.EmployeeId);
        }

        foreach (var employeeId in affectedEmployeeIds)
        {
            var user = await _users.FirstOrDefaultAsync(new UserByEmployeeIdSpecification(tenantId, employeeId), cancellationToken);
            if (user is null)
            {
                continue;
            }

            var message = new NotificationMessage(
                user.Id.Value.ToString(),
                "Your roster has been published",
                $"Your work schedule for {request.RangeStart:yyyy-MM-dd} to {request.RangeEnd:yyyy-MM-dd} is now available.",
                new Dictionary<string, string>());

            await _notificationDispatcher.DispatchAsync(message, cancellationToken);
        }

        return Result.Success(publishedCount);
    }
}
