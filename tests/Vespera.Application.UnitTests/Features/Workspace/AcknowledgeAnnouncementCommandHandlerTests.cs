using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Workspace;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class AcknowledgeAnnouncementCommandHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<AnnouncementReceipt> _receipts = Substitute.For<IReadRepository<AnnouncementReceipt>>();
    private readonly IWriteRepository<AnnouncementReceipt> _receiptWriter = Substitute.For<IWriteRepository<AnnouncementReceipt>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public AcknowledgeAnnouncementCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private AcknowledgeAnnouncementCommandHandler CreateHandler() => new(
        _receipts, _receiptWriter, _tenantContext, _dateTimeProvider, CurrentEmployeeTestSupport.CreateResolver(TenantId, EmployeeId));

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_User_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var resolver = new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), currentUser);
        var handler = new AcknowledgeAnnouncementCommandHandler(_receipts, _receiptWriter, _tenantContext, _dateTimeProvider, resolver);

        var result = await handler.Handle(new AcknowledgeAnnouncementCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("announcement_receipt.no_employee");
    }

    [Fact]
    public async Task Handle_Should_Create_And_Acknowledge_A_New_Receipt_When_None_Exists()
    {
        _receipts.FirstOrDefaultAsync(Arg.Any<AnnouncementReceiptByAnnouncementAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((AnnouncementReceipt?)null);

        var result = await CreateHandler().Handle(new AcknowledgeAnnouncementCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _receiptWriter.Received(1).AddAsync(
            Arg.Is<AnnouncementReceipt>(r => r.EmployeeId == EmployeeId && r.AcknowledgedAt == Now), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Acknowledge_An_Existing_Unacknowledged_Receipt()
    {
        var announcementId = new AnnouncementId(Guid.NewGuid());
        var receipt = AnnouncementReceipt.Create(TenantId, announcementId, EmployeeId);
        _receipts.FirstOrDefaultAsync(Arg.Any<AnnouncementReceiptByAnnouncementAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(receipt);

        var result = await CreateHandler().Handle(new AcknowledgeAnnouncementCommand(announcementId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        receipt.AcknowledgedAt.Should().Be(Now);
        _receiptWriter.Received(1).Update(receipt);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Receipt_Is_Already_Acknowledged()
    {
        var announcementId = new AnnouncementId(Guid.NewGuid());
        var receipt = AnnouncementReceipt.Create(TenantId, announcementId, EmployeeId);
        receipt.Acknowledge(Now.AddDays(-1));
        _receipts.FirstOrDefaultAsync(Arg.Any<AnnouncementReceiptByAnnouncementAndEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(receipt);

        var result = await CreateHandler().Handle(new AcknowledgeAnnouncementCommand(announcementId.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _receiptWriter.DidNotReceive().Update(Arg.Any<AnnouncementReceipt>());
    }
}
