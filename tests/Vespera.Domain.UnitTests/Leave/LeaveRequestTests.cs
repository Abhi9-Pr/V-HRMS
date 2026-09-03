using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Leave;

public class LeaveRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Approve_Should_Raise_LeaveApproved_And_Set_Status()
    {
        var request = CreateRequest();
        var approverId = EmployeeId.New();

        var result = request.Approve(approverId, Now, "manager@vespera.test");

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Approved);
        request.DomainEvents.Should().ContainSingle(e => e is LeaveApproved);
    }

    [Fact]
    public void Reject_Should_Raise_LeaveRejected_And_Set_Status()
    {
        var request = CreateRequest();
        var approverId = EmployeeId.New();

        var result = request.Reject(approverId, "Insufficient coverage", Now, "manager@vespera.test");

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Rejected);
        request.DomainEvents.Should().ContainSingle(e => e is LeaveRejected);
    }

    [Fact]
    public void Approve_Should_Fail_When_Request_Is_Not_Pending()
    {
        var request = CreateRequest();
        request.Approve(EmployeeId.New(), Now, "manager@vespera.test");

        var result = request.Approve(EmployeeId.New(), Now, "manager@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_Should_Succeed_From_Pending_Or_Approved()
    {
        var request = CreateRequest();

        request.Cancel(Now, "employee@vespera.test").IsSuccess.Should().BeTrue();
        request.Status.Should().Be(LeaveRequestStatus.Cancelled);
    }

    [Fact]
    public void FlagLossOfPay_Should_Record_The_Requested_Amount()
    {
        var request = CreateRequest();

        var result = request.FlagLossOfPay(2m);

        result.IsSuccess.Should().BeTrue();
        request.LossOfPayDays.Should().Be(2m);
    }

    [Fact]
    public void FlagLossOfPay_Should_Reject_A_Negative_Amount()
    {
        var request = CreateRequest();

        var result = request.FlagLossOfPay(-1m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Submit_Should_Fail_When_RequestedDays_Is_NonPositive()
    {
        var period = DateRange.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3)).Value;

        var result = LeaveRequest.Submit(
            TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), period, requestedDays: 0m,
            reason: "Personal", Now, "employee@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.invalid_days");
    }

    [Fact]
    public void Submit_Should_Fail_With_A_Blank_Reason()
    {
        var period = DateRange.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3)).Value;

        var result = LeaveRequest.Submit(
            TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), period, requestedDays: 3m,
            reason: "   ", Now, "employee@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.reason_required");
    }

    [Fact]
    public void Reject_Should_Fail_With_A_Blank_Reason()
    {
        var request = CreateRequest();

        var result = request.Reject(EmployeeId.New(), "  ", Now, "manager@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.rejection_reason_required");
    }

    [Fact]
    public void Reject_Should_Fail_When_Request_Is_Not_Pending()
    {
        var request = CreateRequest();
        request.Approve(EmployeeId.New(), Now, "manager@vespera.test");

        var result = request.Reject(EmployeeId.New(), "Too late", Now, "manager@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.not_pending");
    }

    [Fact]
    public void Cancel_Should_Fail_Once_Rejected()
    {
        var request = CreateRequest();
        request.Reject(EmployeeId.New(), "Insufficient coverage", Now, "manager@vespera.test");

        var result = request.Cancel(Now, "employee@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("leave_request.cannot_cancel");
    }

    [Fact]
    public void LeaveRequestId_New_Should_Generate_Distinct_Values()
    {
        LeaveRequestId.New().Should().NotBe(LeaveRequestId.New());
    }

    private static LeaveRequest CreateRequest()
    {
        var period = DateRange.Create(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3)).Value;

        return LeaveRequest.Submit(
            TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), period, requestedDays: 3m,
            reason: "Personal", Now, "employee@vespera.test").Value;
    }
}
