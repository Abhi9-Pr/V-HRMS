using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollRunTests
{
    private static readonly DateTimeOffset Now = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FreezeAttendance_Should_Succeed_On_Or_After_The_Configured_Day_Without_An_Override_Reason()
    {
        var run = OpenRun();

        var result = run.FreezeAttendance(new DateOnly(2026, 2, 25), freezeDay: 25, Now, "hr@vespera.test");

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.AttendanceFrozen);
        run.FreezeOverriddenBy.Should().BeNull();
    }

    [Fact]
    public void FreezeAttendance_Before_The_Configured_Day_Should_Require_An_Override_Reason()
    {
        var run = OpenRun();

        var result = run.FreezeAttendance(new DateOnly(2026, 2, 20), freezeDay: 25, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void FreezeAttendance_Before_The_Configured_Day_With_A_Reason_Should_Record_The_Override()
    {
        var run = OpenRun();

        var result = run.FreezeAttendance(new DateOnly(2026, 2, 20), freezeDay: 25, Now, "hr@vespera.test", "Month-end travel, closing early");

        result.IsSuccess.Should().BeTrue();
        run.FreezeOverriddenBy.Should().Be("hr@vespera.test");
        run.FreezeOverrideReason.Should().Be("Month-end travel, closing early");
        run.DomainEvents.Should().ContainSingle(e => e is PayrollRunAttendanceFrozen);
    }

    [Fact]
    public void FreezeAttendance_Should_Fail_When_Not_In_Draft()
    {
        var run = FrozenRun();

        var result = run.FreezeAttendance(new DateOnly(2026, 2, 25), freezeDay: 25, Now, "hr@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecomputeLines_Should_Fail_While_Still_Draft()
    {
        var run = OpenRun();

        var result = run.RecomputeLines([SampleLine()], "vikram", Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RecomputeLines_Should_Populate_Lines_And_Move_To_DryRun()
    {
        var run = FrozenRun();

        var result = run.RecomputeLines([SampleLine()], "vikram", Now);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.DryRun);
        run.Lines.Should().HaveCount(1);
        run.DryRunExecutedBy.Should().Be("vikram");
        run.DomainEvents.Should().ContainSingle(e => e is PayrollRunDryRunCompleted);
    }

    [Fact]
    public void RecomputeLines_Should_Be_Callable_Again_From_Review_And_Land_Back_In_DryRun()
    {
        var run = InReviewRun();

        var result = run.RecomputeLines([SampleLine(), SampleLine()], "vikram", Now);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.DryRun);
        run.Lines.Should().HaveCount(2, "recomputing must clear the previous lines, not append to them");
    }

    [Fact]
    public void SubmitForReview_Should_Fail_When_Not_In_DryRun()
    {
        var run = FrozenRun();

        var result = run.SubmitForReview();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubmitForReview_Should_Move_To_Review()
    {
        var run = InDryRunRun();

        var result = run.SubmitForReview();

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Review);
    }

    [Fact]
    public void Approve_Should_Fail_When_Not_In_Review()
    {
        var run = InDryRunRun();

        var result = run.Approve("fatima", Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Approve_Should_Move_To_Approved_And_Raise_PayrollRunApproved()
    {
        var run = InReviewRun();

        var result = run.Approve("fatima", Now);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Approved);
        run.DomainEvents.Should().ContainSingle(e => e is PayrollRunApproved);
    }

    [Fact]
    public void RecomputeLines_Should_Fail_Once_Approved()
    {
        var run = ApprovedRun();

        var result = run.RecomputeLines([SampleLine()], "vikram", Now);

        result.IsFailure.Should().BeTrue("an approved run's lines are sealed — corrections are a separate arrears run");
    }

    [Fact]
    public void Finalize_Should_Fail_When_Not_Approved()
    {
        var run = InReviewRun();

        var result = run.Finalize(Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Finalize_Should_Succeed_When_Approved_And_Raise_PayrollFinalized()
    {
        var run = ApprovedRun();

        var result = run.Finalize(Now);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Finalized);
        run.DomainEvents.Should().ContainSingle(e => e is PayrollFinalized);
    }

    [Fact]
    public void Finalize_Should_Fail_When_Already_Finalized()
    {
        var run = ApprovedRun();
        run.Finalize(Now);

        var result = run.Finalize(Now);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Fail_When_Not_Finalized()
    {
        var run = ApprovedRun();

        var result = run.Publish(Now, "fatima");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Publish_Should_Succeed_When_Finalized_And_Raise_PayrollRunPublished()
    {
        var run = ApprovedRun();
        run.Finalize(Now);

        var result = run.Publish(Now, "fatima");

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Published);
        run.DomainEvents.Should().ContainSingle(e => e is PayrollRunPublished);
    }

    private static PayrollLineInput SampleLine() =>
        new(EmployeeId.New(), Money.Of(50000m, Currency.Inr), Money.Of(5000m, Currency.Inr), Money.Of(45000m, Currency.Inr), 0m);

    private static PayrollRun OpenRun() => PayrollRun.Open(TenantId.New(), 2, 2026, Now, "vikram").Value;

    private static PayrollRun FrozenRun()
    {
        var run = OpenRun();
        run.FreezeAttendance(new DateOnly(2026, 2, 25), freezeDay: 25, Now, "hr@vespera.test");
        return run;
    }

    private static PayrollRun InDryRunRun()
    {
        var run = FrozenRun();
        run.RecomputeLines([SampleLine()], "vikram", Now);
        return run;
    }

    private static PayrollRun InReviewRun()
    {
        var run = InDryRunRun();
        run.SubmitForReview();
        return run;
    }

    private static PayrollRun ApprovedRun()
    {
        var run = InReviewRun();
        run.Approve("fatima", Now);
        return run;
    }
}
