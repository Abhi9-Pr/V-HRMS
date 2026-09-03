using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Attendance;

public class PublicHolidayTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateOnly Date = new(2026, 1, 26);

    [Fact]
    public void Create_Should_Succeed_With_A_Valid_Name()
    {
        var result = PublicHoliday.Create(TenantId, Date, "Republic Day");

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.Date.Should().Be(Date);
        result.Value.Name.Should().Be("Republic Day");
    }

    [Fact]
    public void Create_Should_Trim_The_Name()
    {
        var result = PublicHoliday.Create(TenantId, Date, "  Republic Day  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Republic Day");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_Should_Fail_When_Name_Is_Blank(string? name)
    {
        var result = PublicHoliday.Create(TenantId, Date, name!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("public_holiday.name_required");
    }

    [Fact]
    public void Create_Should_Assign_A_New_Id_Each_Time()
    {
        var first = PublicHoliday.Create(TenantId, Date, "Republic Day").Value;
        var second = PublicHoliday.Create(TenantId, Date, "Republic Day").Value;

        first.Id.Should().NotBe(second.Id);
    }
}
