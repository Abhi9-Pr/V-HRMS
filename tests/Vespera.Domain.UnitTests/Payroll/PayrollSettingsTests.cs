using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollSettingsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void Create_Should_Reject_An_Out_Of_Range_Freeze_Day(int freezeDay)
    {
        var result = PayrollSettings.Create(TenantId.New(), freezeDay, 10m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Reject_A_NonPositive_Variance_Threshold()
    {
        var result = PayrollSettings.Create(TenantId.New(), 25, 0m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Update_Should_Replace_Both_Values()
    {
        var settings = PayrollSettings.Create(TenantId.New(), 25, 10m).Value;

        var result = settings.Update(20, 5m);

        result.IsSuccess.Should().BeTrue();
        settings.AttendanceFreezeDay.Should().Be(20);
        settings.VarianceThresholdPercent.Should().Be(5m);
    }
}
