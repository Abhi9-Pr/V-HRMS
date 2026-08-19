using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class EffectiveDatedTests
{
    [Fact]
    public void IsActiveOn_Should_Be_True_Within_An_OpenEnded_Range()
    {
        var item = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), null);

        item.IsActiveOn(new DateOnly(2030, 1, 1)).Should().BeTrue();
    }

    [Fact]
    public void IsActiveOn_Should_Be_False_Before_ValidFrom_Or_After_ValidTo()
    {
        var item = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        item.IsActiveOn(new DateOnly(2025, 12, 31)).Should().BeFalse();
        item.IsActiveOn(new DateOnly(2027, 1, 1)).Should().BeFalse();
        item.IsActiveOn(new DateOnly(2026, 6, 1)).Should().BeTrue();
    }

    [Fact]
    public void Constructing_With_ValidTo_Before_ValidFrom_Should_Throw()
    {
        var constructing = () => new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1));

        constructing.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureNoOverlap_Should_Reject_An_Overlapping_Candidate()
    {
        var existing = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var candidate = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31));

        var result = EffectiveDatedTimeline.EnsureNoOverlap<Guid, TestEffectiveDated>([existing], candidate);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void EnsureNoOverlap_Should_Accept_An_Adjacent_NonOverlapping_Candidate()
    {
        var existing = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var candidate = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31));

        var result = EffectiveDatedTimeline.EnsureNoOverlap<Guid, TestEffectiveDated>([existing], candidate);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void EnsureNoOverlap_Should_Ignore_The_Candidates_Own_Prior_Revision()
    {
        var id = Guid.NewGuid();
        var existing = new TestEffectiveDated(id, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        var result = EffectiveDatedTimeline.EnsureNoOverlap<Guid, TestEffectiveDated>([existing], existing);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AsOf_Should_Return_The_Item_Active_On_The_Given_Date()
    {
        var first = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));
        var second = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 7, 1), null);

        var result = EffectiveDatedTimeline.AsOf<Guid, TestEffectiveDated>([first, second], new DateOnly(2026, 8, 1));

        result.Should().BeSameAs(second);
    }

    [Fact]
    public void AsOf_Should_Return_Null_When_Nothing_Is_Active_On_The_Given_Date()
    {
        var first = new TestEffectiveDated(Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30));

        var result = EffectiveDatedTimeline.AsOf<Guid, TestEffectiveDated>([first], new DateOnly(2025, 1, 1));

        result.Should().BeNull();
    }

    private sealed class TestEffectiveDated(Guid id, DateOnly validFrom, DateOnly? validTo)
        : EffectiveDated<Guid>(id, validFrom, validTo);
}
