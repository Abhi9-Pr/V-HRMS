using FluentAssertions;
using MediatR;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Dashboard.Widgets;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Shifts;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Dashboard.Widgets;

public class ShiftTrackerWidgetProviderTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IReadRepository<ShiftRoster> _shiftRosters = Substitute.For<IReadRepository<ShiftRoster>>();
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ShiftTrackerWidgetProviderTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _shiftRosters.ListAsync(Arg.Any<ShiftRostersByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private ShiftTrackerWidgetProvider CreateProvider() =>
        new(_sender, _shiftRosters, _shifts, _tenantContext, _dateTimeProvider, new CurrentEmployeeResolver(_users, _currentUser));

    private void SignInAs(EmployeeId employeeId)
    {
        var userId = Guid.NewGuid();
        var user = User.Create(TenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, Now, "system");
        _currentUser.UserId.Returns(userId);
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_Unavailable_When_There_Is_No_Signed_In_Employee()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().BeOfType<ShiftTrackerWidgetDto>().Subject;
        dto.PunchStatus.Should().Be("Unavailable");
        dto.EmployeeId.Should().BeNull();
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_NotStarted_When_No_Attendance_Day_Exists_Yet()
    {
        var employeeId = EmployeeId.New();
        SignInAs(employeeId);
        _sender.Send(Arg.Any<GetAttendanceDayQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AttendanceDayDto>(Error.NotFound("attendance_day.not_found", "No attendance day found.")));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().BeOfType<ShiftTrackerWidgetDto>().Subject;
        dto.PunchStatus.Should().Be("NotStarted");
        dto.EmployeeId.Should().Be(employeeId.Value);
        dto.ShiftName.Should().BeNull();
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_In_When_The_Employee_Has_Punched_In_But_Not_Out()
    {
        var employeeId = EmployeeId.New();
        SignInAs(employeeId);
        var day = new AttendanceDayDto(
            Guid.NewGuid(), employeeId.Value, DateOnly.FromDateTime(Now.UtcDateTime), "Open", Now, null, 0, 0, 0, 0, false, null, null);
        _sender.Send(Arg.Any<GetAttendanceDayQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(day));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<ShiftTrackerWidgetDto>().Subject;
        dto.PunchStatus.Should().Be("In");
        dto.FirstIn.Should().Be(Now);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Return_Out_When_The_Employee_Has_Punched_In_And_Out()
    {
        var employeeId = EmployeeId.New();
        SignInAs(employeeId);
        var day = new AttendanceDayDto(
            Guid.NewGuid(), employeeId.Value, DateOnly.FromDateTime(Now.UtcDateTime), "Closed", Now, Now.AddHours(8), 480, 0, 0, 0, false, null, null);
        _sender.Send(Arg.Any<GetAttendanceDayQuery>(), Arg.Any<CancellationToken>()).Returns(Result.Success(day));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<ShiftTrackerWidgetDto>().Subject;
        dto.PunchStatus.Should().Be("Out");
        dto.WorkedMinutes.Should().Be(480);
    }

    [Fact]
    public async Task GetPayloadAsync_Should_Include_The_Resolved_Shifts_Name_And_Times_When_A_Roster_Assignment_Exists()
    {
        var employeeId = EmployeeId.New();
        SignInAs(employeeId);
        var shift = Shift.Create(TenantId, "Morning Shift", new TimeOnly(9, 0), new TimeOnly(17, 0), 10, Now, "hr").Value;
        var period = DateRange.Create(DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1), DateOnly.FromDateTime(Now.UtcDateTime).AddDays(30)).Value;
        var roster = ShiftRoster.Create(TenantId, employeeId, shift.Id, period, Now);
        roster.Publish(Now, "hr");

        _shiftRosters.ListAsync(Arg.Any<ShiftRostersByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns([roster]);
        _shifts.FirstOrDefaultAsync(Arg.Any<ShiftByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(shift);
        _sender.Send(Arg.Any<GetAttendanceDayQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<AttendanceDayDto>(Error.NotFound("attendance_day.not_found", "No attendance day found.")));

        var result = await CreateProvider().GetPayloadAsync(CancellationToken.None);

        var dto = result.Value.Should().BeOfType<ShiftTrackerWidgetDto>().Subject;
        dto.ShiftName.Should().Be("Morning Shift");
        dto.ShiftStart.Should().Be(new TimeOnly(9, 0));
        dto.ShiftEnd.Should().Be(new TimeOnly(17, 0));
    }
}
