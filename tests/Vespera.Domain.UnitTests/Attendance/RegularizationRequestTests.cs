using FluentAssertions;
using Vespera.Domain.Attendance;
using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Attendance;

public sealed class RegularizationRequestTests
{
    private static readonly TenantId TenantId = new(Guid.NewGuid());
    private static readonly EmployeeId EmployeeId = new(Guid.NewGuid());
    private static readonly EmployeeId ApproverId = new(Guid.NewGuid());
    private static readonly AttendanceDayId AttendanceDayId = AttendanceDayId.New();
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Submit_Should_Fail_When_Reason_Is_Blank()
    {
        var result = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.reason_required");
    }

    [Fact]
    public void Submit_Should_Succeed_With_Optional_Evidence()
    {
        var result = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "Forgot to punch out", "storage/key-1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RegularizationStatus.Pending);
        result.Value.EvidenceFileReference.Should().Be("storage/key-1");
    }

    [Fact]
    public void Approve_Should_Raise_RegularizationApproved_And_Set_Status()
    {
        var request = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "Forgot to punch out").Value;

        var result = request.Approve(ApproverId, Now);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(RegularizationStatus.Approved);
        request.ApproverId.Should().Be(ApproverId);
        request.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RegularizationApproved>();
        var raised = (RegularizationApproved)request.DomainEvents.Single();
        raised.RequestId.Should().Be(request.Id);
        raised.EmployeeId.Should().Be(EmployeeId);
        raised.AttendanceDayId.Should().Be(AttendanceDayId);
    }

    [Fact]
    public void Approve_Should_Fail_When_Not_Pending()
    {
        var request = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "Forgot to punch out").Value;
        request.Approve(ApproverId, Now);

        var result = request.Approve(ApproverId, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.not_pending");
    }

    [Fact]
    public void Reject_Should_Raise_RegularizationRejected_And_Set_Status()
    {
        var request = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "Forgot to punch out").Value;

        var result = request.Reject(ApproverId, "Not supported by evidence", Now);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(RegularizationStatus.Rejected);
        request.RejectionReason.Should().Be("Not supported by evidence");
        request.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<RegularizationRejected>();
    }

    [Fact]
    public void Reject_Should_Fail_When_Reason_Is_Blank()
    {
        var request = RegularizationRequest.Submit(TenantId, EmployeeId, AttendanceDayId, "Forgot to punch out").Value;

        var result = request.Reject(ApproverId, " ", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("regularization_request.rejection_reason_required");
        request.DomainEvents.Should().BeEmpty();
    }
}
