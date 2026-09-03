using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class GetAnnouncementReceiptsReportQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<Announcement> _announcements = Substitute.For<IReadRepository<Announcement>>();
    private readonly IReadRepository<AnnouncementReceipt> _receipts = Substitute.For<IReadRepository<AnnouncementReceipt>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    public GetAnnouncementReceiptsReportQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetAnnouncementReceiptsReportQueryHandler CreateHandler() => new(_announcements, _receipts, _employees, _tenantContext);

    private static Announcement CreateAnnouncement() => Announcement.Create(
        TenantId, "Holiday notice", "Office closed", AnnouncementAudienceScope.AllEmployees, null, null, AnnouncementPriority.Normal,
        Now, null, Now, "hr@vespera.test").Value;

    private static Employee CreateEmployee(string firstName, string lastName) => Employee.Onboard(
        TenantId, EmployeeCode.Create($"EMP-{Guid.NewGuid():N}"[..12]).Value, firstName, lastName,
        EmailAddress.Create($"{Guid.NewGuid():N}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "seed").Value;

    [Fact]
    public async Task Handle_Should_Fail_When_The_Announcement_Does_Not_Exist()
    {
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((Announcement?)null);

        var result = await CreateHandler().Handle(new GetAnnouncementReceiptsReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement.not_found");
    }

    [Fact]
    public async Task Handle_Should_Build_A_Report_Row_Per_Target_Employee_With_Acknowledgement_Status()
    {
        var announcement = CreateAnnouncement();
        _announcements.FirstOrDefaultAsync(Arg.Any<AnnouncementByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(announcement);

        var acknowledgedEmployee = CreateEmployee("Zed", "Zephyr");
        var pendingEmployee = CreateEmployee("Amy", "Adams");
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[acknowledgedEmployee, pendingEmployee]);

        var receipt = AnnouncementReceipt.Create(TenantId, announcement.Id, acknowledgedEmployee.Id);
        receipt.Acknowledge(Now);
        _receipts.ListAsync(Arg.Any<AnnouncementReceiptsByAnnouncementSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AnnouncementReceipt>)[receipt]);

        var result = await CreateHandler().Handle(new GetAnnouncementReceiptsReportQuery(announcement.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TargetEmployeeCount.Should().Be(2);
        result.Value.AcknowledgedCount.Should().Be(1);
        result.Value.Rows.Should().HaveCount(2);
        result.Value.Rows[0].EmployeeName.Should().Be("Amy Adams");
        result.Value.Rows[0].Acknowledged.Should().BeFalse();
        result.Value.Rows[1].EmployeeName.Should().Be("Zed Zephyr");
        result.Value.Rows[1].Acknowledged.Should().BeTrue();
    }
}
