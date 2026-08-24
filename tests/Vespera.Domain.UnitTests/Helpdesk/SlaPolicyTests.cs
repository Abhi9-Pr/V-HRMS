using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Helpdesk;

namespace Vespera.Domain.UnitTests.Helpdesk;

public class SlaPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Fail_When_Resolution_Time_Is_Shorter_Than_Response_Time()
    {
        var result = SlaPolicy.Create(
            TenantId.New(), "Standard", TimeSpan.FromHours(4), TimeSpan.FromHours(2), Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Default_Business_Hours_When_Not_Specified()
    {
        var policy = SlaPolicy.Create(
            TenantId.New(), "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(24), Now, "admin@vespera.test").Value;

        policy.BusinessHoursStart.Should().Be(new TimeOnly(9, 0));
        policy.BusinessHoursEnd.Should().Be(new TimeOnly(18, 0));
    }

    [Fact]
    public void Create_Should_Accept_Explicit_Business_Hours()
    {
        var policy = SlaPolicy.Create(
            TenantId.New(), "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(24), Now, "admin@vespera.test",
            new TimeOnly(8, 30), new TimeOnly(17, 30)).Value;

        policy.BusinessHoursStart.Should().Be(new TimeOnly(8, 30));
        policy.BusinessHoursEnd.Should().Be(new TimeOnly(17, 30));
    }

    [Fact]
    public void UpdateTargets_Should_Fail_When_Resolution_Time_Is_Shorter_Than_Response_Time()
    {
        var policy = SlaPolicy.Create(
            TenantId.New(), "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(24), Now, "admin@vespera.test").Value;

        var result = policy.UpdateTargets(TimeSpan.FromHours(4), TimeSpan.FromHours(2), Now.AddMinutes(1), "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateTargets_Should_Update_Business_Hours_When_Provided()
    {
        var policy = SlaPolicy.Create(
            TenantId.New(), "Standard", TimeSpan.FromHours(2), TimeSpan.FromHours(24), Now, "admin@vespera.test").Value;

        policy.UpdateTargets(
            TimeSpan.FromHours(1), TimeSpan.FromHours(12), Now.AddMinutes(1), "admin@vespera.test", new TimeOnly(8, 0), new TimeOnly(20, 0));

        policy.BusinessHoursStart.Should().Be(new TimeOnly(8, 0));
        policy.BusinessHoursEnd.Should().Be(new TimeOnly(20, 0));
    }
}
