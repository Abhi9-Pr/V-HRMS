using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Recruitment;
using Vespera.Application.UnitTests.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class SubmitRequisitionForApprovalCommandHandlerTests
{
    private readonly IReadRepository<JobRequisition> _requisitionReads = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IWriteRepository<JobRequisition> _requisitions = Substitute.For<IWriteRepository<JobRequisition>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly IWriteRepository<ApprovalChain> _approvalChains = Substitute.For<IWriteRepository<ApprovalChain>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _submitterId = EmployeeId.New();
    private readonly EmployeeId _managerId = EmployeeId.New();

    public SubmitRequisitionForApprovalCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private JobRequisition CreateDraftRequisition()
    {
        var requisition = JobRequisition.Create(_tenantId, "Senior Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        return requisition;
    }

    private SubmitRequisitionForApprovalCommandHandler CreateHandler() => new(
        _requisitionReads, _requisitions, _reportingRelationships, _proxyDelegations, _approvalChains, _tenantContext, _dateTimeProvider,
        _notificationDispatcher, CurrentEmployeeTestSupport.CreateResolver(_tenantId, _submitterId));

    [Fact]
    public async Task Handle_Should_Submit_And_Open_An_Approval_Chain_Against_The_Submitters_Manager()
    {
        var requisition = CreateDraftRequisition();
        var relationship = ReportingRelationship.Create(_tenantId, _submitterId, _managerId, new DateOnly(2020, 1, 1), null).Value;
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveReportingRelationshipByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns(relationship);

        var result = await CreateHandler().Handle(new SubmitRequisitionForApprovalCommand(requisition.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.PendingApproval);
        await _approvalChains.Received(1).AddAsync(
            Arg.Is<ApprovalChain>(c => c.SubjectType == ApprovalSubjectType.JobRequisition && c.CurrentStep.ApproverId == _managerId),
            Arg.Any<CancellationToken>());
        await _notificationDispatcher.Received(1).DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Submitter_Has_No_Active_Manager()
    {
        var requisition = CreateDraftRequisition();
        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ActiveReportingRelationshipByEmployeeSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ReportingRelationship?)null);

        var result = await CreateHandler().Handle(new SubmitRequisitionForApprovalCommand(requisition.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _approvalChains.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Requisition_Is_Not_Found()
    {
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((JobRequisition?)null);

        var result = await CreateHandler().Handle(new SubmitRequisitionForApprovalCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
