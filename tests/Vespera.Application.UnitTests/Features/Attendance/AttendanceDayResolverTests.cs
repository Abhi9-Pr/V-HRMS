using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Attendance;

/// <summary>Direct proof of the overnight-crossing mechanism behind the "night shift crossing
/// midnight computes correctly" requirement: a night shift's closing punch, whose *local calendar
/// date* is already "tomorrow," must still land on the AttendanceDay opened the night before.</summary>
public class AttendanceDayResolverTests
{
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private static readonly DateOnly Today = new(2026, 1, 16);
    private static readonly DateOnly Yesterday = new(2026, 1, 15);

    [Fact]
    public async Task ResolveAsync_Should_Open_A_New_Day_When_Neither_Today_Nor_Yesterday_Has_A_Row()
    {
        StubDate(Today, null);
        StubDate(Yesterday, null);

        var (day, isNew) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, _tenantId, _employeeId, Today, CancellationToken.None);

        isNew.Should().BeTrue();
        day.Date.Should().Be(Today);
    }

    [Fact]
    public async Task ResolveAsync_Should_Return_Todays_Row_When_One_Already_Exists()
    {
        var existing = AttendanceDay.Open(_tenantId, _employeeId, Today);
        StubDate(Today, existing);
        StubDate(Yesterday, null);

        var (day, isNew) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, _tenantId, _employeeId, Today, CancellationToken.None);

        isNew.Should().BeFalse();
        day.Should().BeSameAs(existing);
    }

    [Fact]
    public async Task ResolveAsync_Should_Return_Yesterdays_Open_Day_When_Today_Has_No_Row()
    {
        var openYesterday = AttendanceDay.Open(_tenantId, _employeeId, Yesterday);
        openYesterday.RecordPunch(PunchType.In, new DateTimeOffset(2026, 1, 15, 22, 5, 0, TimeSpan.Zero), null, PunchSource.Web);
        openYesterday.IsOpen.Should().BeTrue();

        StubDate(Today, null);
        StubDate(Yesterday, openYesterday);

        var (day, isNew) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, _tenantId, _employeeId, Today, CancellationToken.None);

        isNew.Should().BeFalse();
        day.Should().BeSameAs(openYesterday);
        day.Date.Should().Be(Yesterday); // the shift's start date, not today's calendar date
    }

    [Fact]
    public async Task ResolveAsync_Should_Not_Reuse_Yesterdays_Day_When_It_Is_Already_Closed()
    {
        var closedYesterday = AttendanceDay.Open(_tenantId, _employeeId, Yesterday);
        closedYesterday.RecordPunch(PunchType.In, new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero), null, PunchSource.Web);
        closedYesterday.RecordPunch(PunchType.Out, new DateTimeOffset(2026, 1, 15, 18, 0, 0, TimeSpan.Zero), null, PunchSource.Web);
        closedYesterday.IsOpen.Should().BeFalse();

        StubDate(Today, null);
        StubDate(Yesterday, closedYesterday);

        var (day, isNew) = await AttendanceDayResolver.ResolveAsync(_attendanceDays, _tenantId, _employeeId, Today, CancellationToken.None);

        isNew.Should().BeTrue();
        day.Date.Should().Be(Today);
    }

    /// <summary>Matches the specific <see cref="AttendanceDayByEmployeeAndDateSpecification"/>
    /// instance built for <paramref name="date"/> by evaluating its compiled predicate against a
    /// probe row for this test's own tenant/employee — <c>ResolveAsync</c> constructs a fresh
    /// specification per call, so NSubstitute can't match on reference equality.</summary>
    private void StubDate(DateOnly date, AttendanceDay? result)
    {
        var probe = AttendanceDay.Open(_tenantId, _employeeId, date);
        _attendanceDays.FirstOrDefaultAsync(
            Arg.Is<ISpecification<AttendanceDay>>(spec => spec.Criteria!.Compile()(probe)),
            Arg.Any<CancellationToken>()).Returns(result);
    }
}
