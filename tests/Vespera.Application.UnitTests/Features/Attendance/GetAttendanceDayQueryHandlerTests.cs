using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetAttendanceDayQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateOnly Date = new(2026, 1, 15);

    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public GetAttendanceDayQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetAttendanceDayQueryHandler CreateHandler() => new(_attendanceDays, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_The_Mapped_Dto_When_The_Day_Exists()
    {
        var employeeId = EmployeeId.New();
        var day = AttendanceDay.Open(TenantId, employeeId, Date);
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<AttendanceDayByEmployeeAndDateSpecification>(), Arg.Any<CancellationToken>()).Returns(day);

        var result = await CreateHandler().Handle(new GetAttendanceDayQuery(employeeId.Value, Date), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmployeeId.Should().Be(employeeId.Value);
        result.Value.Date.Should().Be(Date);
        result.Value.Id.Should().Be(day.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Attendance_Day_Exists()
    {
        _attendanceDays.FirstOrDefaultAsync(Arg.Any<AttendanceDayByEmployeeAndDateSpecification>(), Arg.Any<CancellationToken>())
            .Returns((AttendanceDay?)null);

        var result = await CreateHandler().Handle(new GetAttendanceDayQuery(Guid.NewGuid(), Date), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("attendance_day.not_found");
    }
}
