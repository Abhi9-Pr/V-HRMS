using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class GenerateRosterCommandHandlerTests
{
    private readonly IReadRepository<RotationPattern> _rotationPatterns = Substitute.For<IReadRepository<RotationPattern>>();
    private readonly IWriteRepository<ShiftRoster> _rosters = Substitute.For<IWriteRepository<ShiftRoster>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly ShiftId DayShift = ShiftId.New();
    private static readonly ShiftId NightShift = ShiftId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public GenerateRosterCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private GenerateRosterCommandHandler CreateHandler() =>
        new(_rotationPatterns, _rosters, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Compress_A_Six_Day_Same_Shift_Run_And_A_Four_Day_Run_Into_Two_Rows()
    {
        // Pattern: 10-day cycle, first 6 days DayShift, last 4 days NightShift.
        var days = Enumerable.Range(0, 10)
            .Select(sequence => new RotationPatternDay(sequence, sequence < 6 ? DayShift : NightShift))
            .ToList();
        var pattern = RotationPattern.Create(TenantId, "Two Blocks", days, Now, "hr@vespera.test").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns(pattern);

        var anchor = new DateOnly(2026, 3, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [EmployeeId.Value], anchor, anchor.AddDays(9), anchor);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        await _rosters.Received(1).AddAsync(Arg.Is<ShiftRoster>(r => r.ShiftId == DayShift), Arg.Any<CancellationToken>());
        await _rosters.Received(1).AddAsync(Arg.Is<ShiftRoster>(r => r.ShiftId == NightShift), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Not_Compress_Alternating_Shifts_Into_One_Row()
    {
        // Pattern: 2-day cycle alternating DayShift/NightShift.
        var days = new List<RotationPatternDay> { new(0, DayShift), new(1, NightShift) };
        var pattern = RotationPattern.Create(TenantId, "Alternating", days, Now, "hr@vespera.test").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns(pattern);

        var anchor = new DateOnly(2026, 3, 1);
        // 10 days alternating -> 10 separate single-day runs, none compressible.
        var command = new GenerateRosterCommand(Guid.NewGuid(), [EmployeeId.Value], anchor, anchor.AddDays(9), anchor);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(10);
    }

    [Fact]
    public async Task Handle_Should_Skip_Off_Days_And_Not_Create_Rows_For_Them()
    {
        // Pattern: 3-day cycle, day 0 DayShift, day 1 off (null), day 2 DayShift.
        var days = new List<RotationPatternDay> { new(0, DayShift), new(1, null), new(2, DayShift) };
        var pattern = RotationPattern.Create(TenantId, "With Off Day", days, Now, "hr@vespera.test").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns(pattern);

        var anchor = new DateOnly(2026, 3, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [EmployeeId.Value], anchor, anchor.AddDays(2), anchor);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Day 0 (shift), day 1 (off, no row), day 2 (shift) -- two separate single-day rows since
        // the off day breaks the run, not one three-day row.
        result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_Wrap_The_Pattern_Across_A_Range_Longer_Than_The_Cycle()
    {
        // Pattern: 3-day cycle, all DayShift -- a 9-day range should still compress into one row
        // (pattern wraparound repeating the same shift doesn't break the run).
        var days = new List<RotationPatternDay> { new(0, DayShift), new(1, DayShift), new(2, DayShift) };
        var pattern = RotationPattern.Create(TenantId, "All Same", days, Now, "hr@vespera.test").Value;
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns(pattern);

        var anchor = new DateOnly(2026, 3, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [EmployeeId.Value], anchor, anchor.AddDays(8), anchor);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Pattern_Does_Not_Exist()
    {
        _rotationPatterns.FirstOrDefaultAsync(Arg.Any<ISpecification<RotationPattern>>(), Arg.Any<CancellationToken>())
            .Returns((RotationPattern?)null);

        var anchor = new DateOnly(2026, 3, 1);
        var command = new GenerateRosterCommand(Guid.NewGuid(), [EmployeeId.Value], anchor, anchor, anchor);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("rotation_pattern.not_found");
        await _rosters.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
