using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Compliance;

namespace Vespera.Domain.UnitTests.Compliance;

public class RetentionPolicyTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_For_Valid_Input()
    {
        var result = RetentionPolicy.Create(
            TenantId, "Employee.Document", retentionPeriodDays: 2555, RetentionAction.Anonymize, Now, "system");

        result.IsSuccess.Should().BeTrue();
        result.Value.EntityCategory.Should().Be("Employee.Document");
        result.Value.RetentionPeriodDays.Should().Be(2555);
        result.Value.Action.Should().Be(RetentionAction.Anonymize);
    }

    [Fact]
    public void Create_Should_Fail_When_EntityCategory_Is_Empty()
    {
        var result = RetentionPolicy.Create(TenantId, "  ", 30, RetentionAction.Purge, Now, "system");

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Fail_When_RetentionPeriodDays_Is_Not_Positive(int retentionPeriodDays)
    {
        var result = RetentionPolicy.Create(TenantId, "Attendance.Punch", retentionPeriodDays, RetentionAction.Purge, Now, "system");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reconfigure_Should_Update_Period_And_Action()
    {
        var policy = RetentionPolicy.Create(TenantId, "Attendance.Punch", 90, RetentionAction.Purge, Now, "system").Value;

        var result = policy.Reconfigure(180, RetentionAction.Anonymize, Now, "system");

        result.IsSuccess.Should().BeTrue();
        policy.RetentionPeriodDays.Should().Be(180);
        policy.Action.Should().Be(RetentionAction.Anonymize);
    }
}
