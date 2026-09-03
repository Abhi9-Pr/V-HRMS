using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Workspace;

namespace Vespera.Domain.UnitTests.Workspace;

public class AnnouncementReceiptTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly AnnouncementId AnnouncementId = AnnouncementId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Leave_AcknowledgedAt_Null()
    {
        var receipt = CreateReceipt();

        receipt.TenantId.Should().Be(TenantId);
        receipt.AnnouncementId.Should().Be(AnnouncementId);
        receipt.EmployeeId.Should().Be(EmployeeId);
        receipt.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void Acknowledge_Should_Set_AcknowledgedAt()
    {
        var receipt = CreateReceipt();

        var result = receipt.Acknowledge(Now);

        result.IsSuccess.Should().BeTrue();
        receipt.AcknowledgedAt.Should().Be(Now);
    }

    [Fact]
    public void Acknowledge_Should_Fail_When_Already_Acknowledged()
    {
        var receipt = CreateReceipt();
        receipt.Acknowledge(Now);

        var result = receipt.Acknowledge(Now.AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        receipt.AcknowledgedAt.Should().Be(Now);
    }

    private static AnnouncementReceipt CreateReceipt() => AnnouncementReceipt.Create(TenantId, AnnouncementId, EmployeeId);
}
