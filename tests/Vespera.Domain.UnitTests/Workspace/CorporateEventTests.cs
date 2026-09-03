using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class CorporateEventTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_With_Valid_Window()
    {
        var result = CreateResult();

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCancelled.Should().BeFalse();
    }

    [Fact]
    public void Create_Should_Reject_Blank_Title()
    {
        var result = CorporateEvent.Create(
            TenantId, "   ", "All-hands", Now.AddDays(1), Now.AddDays(1).AddHours(2), "HQ", Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Reject_An_End_At_Or_Before_The_Start()
    {
        var result = CorporateEvent.Create(
            TenantId, "All-hands", "All-hands", Now.AddDays(1), Now.AddDays(1), "HQ", Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Reschedule_Should_Update_The_Window()
    {
        var corporateEvent = CreateResult().Value;
        var newStart = Now.AddDays(2);
        var newEnd = newStart.AddHours(3);

        var result = corporateEvent.Reschedule(newStart, newEnd, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        corporateEvent.StartsAt.Should().Be(newStart);
        corporateEvent.EndsAt.Should().Be(newEnd);
    }

    [Fact]
    public void Reschedule_Should_Reject_An_End_At_Or_Before_The_Start()
    {
        var corporateEvent = CreateResult().Value;

        var result = corporateEvent.Reschedule(Now.AddDays(2), Now.AddDays(2), Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_Should_Set_IsCancelled()
    {
        var corporateEvent = CreateResult().Value;

        var result = corporateEvent.Cancel(Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        corporateEvent.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Cancel_Should_Fail_When_Already_Cancelled()
    {
        var corporateEvent = CreateResult().Value;
        corporateEvent.Cancel(Now, "hr@vespera.test");

        var result = corporateEvent.Cancel(Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    private static Result<CorporateEvent> CreateResult() =>
        CorporateEvent.Create(TenantId, "All-hands", "Quarterly all-hands", Now.AddDays(1), Now.AddDays(1).AddHours(2), "HQ", Now, "hr@vespera.test");
}
