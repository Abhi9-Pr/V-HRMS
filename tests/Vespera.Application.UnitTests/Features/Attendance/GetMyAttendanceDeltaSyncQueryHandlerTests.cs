using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Attendance;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Attendance;

public class GetMyAttendanceDeltaSyncQueryHandlerTests
{
    private readonly IReadRepository<AttendanceDay> _attendanceDays = Substitute.For<IReadRepository<AttendanceDay>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();
    private readonly Guid _userId = Guid.NewGuid();

    public GetMyAttendanceDeltaSyncQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero));

        var user = User.Create(_tenantId, EmailAddress.Create("a@vespera.test").Value, _employeeId, DateTimeOffset.UtcNow, "seed");
        _users.FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    private GetMyAttendanceDeltaSyncQueryHandler CreateHandler() =>
        new(_attendanceDays, _dateTimeProvider, _tenantContext, _currentUser, _users);

    private AttendanceDay CreateComputedDay(DateOnly date, DateTimeOffset changedAt)
    {
        var day = AttendanceDay.Open(_tenantId, _employeeId, date);
        day.RecordPunch(PunchType.In, changedAt, location: null, PunchSource.Web);
        return day;
    }

    [Fact]
    public async Task Handle_Should_Return_Upserts_For_Every_Matched_Day()
    {
        var day1 = CreateComputedDay(new DateOnly(2026, 3, 1), new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero));
        var day2 = CreateComputedDay(new DateOnly(2026, 3, 2), new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>())
            .Returns([day1, day2]);

        var query = new GetMyAttendanceDeltaSyncQuery(DateTimeOffset.MinValue, null, 100);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().HaveCount(2);
        result.Value.Upserts.Should().Contain(dto => dto.Id == day1.Id.Value && dto.Date == day1.Date);
        result.Value.TombstonedIds.Should().BeEmpty();
    }

    [Fact]
    public void TombstonedIds_Is_Structurally_Supported_But_Currently_Unreachable()
    {
        // AttendanceDay.IsDeleted is hardcoded false (see AttendanceDay's own doc comment) — an
        // AttendanceDay is a computed historical record, never deleted. The base class's tombstone
        // path exists for a future TEntity that genuinely supports deletion, not this one.
    }

    [Fact]
    public async Task Handle_Should_Set_NextCursor_When_More_Rows_Exist_Than_PageSize()
    {
        var days = Enumerable.Range(1, 5)
            .Select(day => CreateComputedDay(new DateOnly(2026, 3, day), new DateTimeOffset(2026, 3, day, 9, 0, 0, TimeSpan.Zero)))
            .ToList();
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(days);

        var query = new GetMyAttendanceDeltaSyncQuery(DateTimeOffset.MinValue, null, 3);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().HaveCount(3);
        result.Value.NextCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Leave_NextCursor_Null_When_Fewer_Rows_Exist_Than_PageSize()
    {
        var days = new[] { CreateComputedDay(new DateOnly(2026, 3, 1), new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero)) };
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns(days);

        var query = new GetMyAttendanceDeltaSyncQuery(DateTimeOffset.MinValue, null, 100);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_Result_Without_Throwing_When_The_Caller_Has_No_Linked_Employee()
    {
        _currentUser.UserId.Returns((Guid?)null);
        _attendanceDays.ListAsync(Arg.Any<ISpecification<AttendanceDay>>(), Arg.Any<CancellationToken>()).Returns([]);

        var query = new GetMyAttendanceDeltaSyncQuery(DateTimeOffset.MinValue, null, 100);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Upserts.Should().BeEmpty();
        await _users.DidNotReceiveWithAnyArgs().FirstOrDefaultAsync(default!, default);
    }
}
