using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Workspace;
using EmployeeByIdSpecification = Vespera.Application.Features.Employees.EmployeeByIdSpecification;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class GetAnnouncementsForMeQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<Announcement> _announcements = Substitute.For<IReadRepository<Announcement>>();
    private readonly IReadRepository<AnnouncementReceipt> _receipts = Substitute.For<IReadRepository<AnnouncementReceipt>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public GetAnnouncementsForMeQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
        _receipts.ListAsync(Arg.Any<AnnouncementReceiptsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AnnouncementReceipt>)[]);
    }

    private GetAnnouncementsForMeQueryHandler CreateHandler() => new(
        _announcements, _receipts, _employees, _tenantContext, _dateTimeProvider,
        CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    private static Employee CreateEmployee() => Employee.Onboard(
        TenantId, EmployeeCode.Create("EMP-100").Value, "Alan", "Turing",
        EmailAddress.Create("alan@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "seed").Value;

    private static Announcement CreateAnnouncement(
        AnnouncementAudienceScope scope = AnnouncementAudienceScope.AllEmployees, DepartmentId? departmentId = null,
        LocationId? locationId = null, AnnouncementPriority priority = AnnouncementPriority.Normal) =>
        Announcement.Create(
            TenantId, "Holiday notice", "Office closed", scope, departmentId, locationId, priority, Now, null, Now, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new GetAnnouncementsForMeQueryHandler(_announcements, _receipts, _employees, _tenantContext, _dateTimeProvider, resolver);

        var result = await handler.Handle(new GetAnnouncementsForMeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_The_Employee_Record_Is_Not_Found()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await CreateHandler().Handle(new GetAnnouncementsForMeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Exclude_Announcements_Outside_The_Employees_Audience()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);

        var forMe = CreateAnnouncement();
        var forOtherDepartment = CreateAnnouncement(AnnouncementAudienceScope.Department, DepartmentId.New());
        _announcements.ListAsync(Arg.Any<PublishedAnnouncementsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Announcement>)[forMe, forOtherDepartment]);

        var result = await CreateHandler().Handle(new GetAnnouncementsForMeQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto => dto.Id == forMe.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Mark_Announcements_Acknowledged_From_The_Employees_Receipts()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);

        var announcement = CreateAnnouncement();
        _announcements.ListAsync(Arg.Any<PublishedAnnouncementsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Announcement>)[announcement]);

        var receipt = AnnouncementReceipt.Create(TenantId, announcement.Id, EmployeeId);
        receipt.Acknowledge(Now);
        _receipts.ListAsync(Arg.Any<AnnouncementReceiptsByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<AnnouncementReceipt>)[receipt]);

        var result = await CreateHandler().Handle(new GetAnnouncementsForMeQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle(dto => dto.IsAcknowledged);
    }

    [Fact]
    public async Task Handle_Should_Order_Pinned_Announcements_Before_Unpinned()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);

        var unpinned = CreateAnnouncement();
        var pinned = CreateAnnouncement();
        pinned.Pin(Now, "hr@vespera.test");
        _announcements.ListAsync(Arg.Any<PublishedAnnouncementsSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Announcement>)[unpinned, pinned]);

        var result = await CreateHandler().Handle(new GetAnnouncementsForMeQuery(), CancellationToken.None);

        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(pinned.Id.Value);
    }
}
