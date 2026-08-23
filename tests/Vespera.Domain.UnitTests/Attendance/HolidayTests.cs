using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Attendance;

public class HolidayTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_With_A_Valid_Name()
    {
        var result = Holiday.Create(TenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Republic Day");
    }

    [Fact]
    public void Create_Should_Fail_With_A_Blank_Name()
    {
        var result = Holiday.Create(TenantId, LocationId.New(), new DateOnly(2026, 1, 26), "  ", Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Rename_Should_Update_The_Name()
    {
        var holiday = Holiday.Create(TenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", Now, "hr@vespera.test").Value;

        var result = holiday.Rename("Republic Day (Observed)", Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        holiday.Name.Should().Be("Republic Day (Observed)");
    }

    [Fact]
    public void Rename_Should_Reject_A_Blank_Name()
    {
        var holiday = Holiday.Create(TenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", Now, "hr@vespera.test").Value;

        var result = holiday.Rename(" ", Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
        holiday.Name.Should().Be("Republic Day");
    }

    [Fact]
    public void Reschedule_Should_Update_The_Date()
    {
        var holiday = Holiday.Create(TenantId, LocationId.New(), new DateOnly(2026, 1, 26), "Republic Day", Now, "hr@vespera.test").Value;

        var result = holiday.Reschedule(new DateOnly(2026, 1, 27), Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        holiday.Date.Should().Be(new DateOnly(2026, 1, 27));
    }
}
