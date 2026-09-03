using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class ClearPunchFlagCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateOnly Date = new(2026, 1, 15);
    private static readonly DateTimeOffset PunchedAt = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IWriteRepository<AttendanceDay> _attendanceDayWriter = Substitute.For<IWriteRepository<AttendanceDay>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public ClearPunchFlagCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private ClearPunchFlagCommandHandler CreateHandler() => new(_attendanceDays, _attendanceDayWriter, _tenantContext);

    private static AttendanceDay CreateDayWithFlaggedPunch(EmployeeId employeeId, out AttendancePunchId punchId)
    {
        var day = AttendanceDay.Open(TenantId, employeeId, Date);
        day.RecordPunch(PunchType.In, PunchedAt, null, PunchSource.Web, requiresApproval: true, flagReason: "Late punch");
        punchId = day.Punches.Single().Id;
        return day;
    }

    [Fact]
    public async Task Handle_Should_Clear_The_Flag_And_Persist_When_The_Punch_Exists()
    {
        var employeeId = EmployeeId.New();
        var day = CreateDayWithFlaggedPunch(employeeId, out var punchId);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<AttendanceDayByEmployeeAndDateSpecification>(), Arg.Any<CancellationToken>()).Returns(day);

        var result = await CreateHandler().Handle(
            new ClearPunchFlagCommand(employeeId.Value, Date, punchId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        day.Punches.Single().RequiresApproval.Should().BeFalse();
        _attendanceDayWriter.Received(1).Update(day);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Attendance_Day_Exists_For_The_Employee_And_Date()
    {
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<AttendanceDayByEmployeeAndDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);

        var result = await CreateHandler().Handle(
            new ClearPunchFlagCommand(EmployeeId.New().Value, Date, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("attendance_day.not_found");
        _attendanceDayWriter.DidNotReceive().Update(Arg.Any<AttendanceDay>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Punch_Does_Not_Exist_On_The_Day()
    {
        var employeeId = EmployeeId.New();
        var day = AttendanceDay.Open(TenantId, employeeId, Date);
        day.RecordPunch(PunchType.In, PunchedAt, null, PunchSource.Web);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<AttendanceDayByEmployeeAndDateSpecification>(), Arg.Any<CancellationToken>()).Returns(day);

        var result = await CreateHandler().Handle(
            new ClearPunchFlagCommand(employeeId.Value, Date, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("attendance_punch.not_found");
        _attendanceDayWriter.DidNotReceive().Update(Arg.Any<AttendanceDay>());
    }
}
