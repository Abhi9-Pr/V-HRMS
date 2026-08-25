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

public class DecideRequisitionApprovalCommandHandlerTests
{
    private readonly IReadRepository<ApprovalChain> _approvalChains = Substitute.For<IReadRepository<ApprovalChain>>();
    private readonly IReadRepository<JobRequisition> _requisitionReads = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IWriteRepository<JobRequisition> _requisitions = Substitute.For<IWriteRepository<JobRequisition>>();
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations = Substitute.For<IReadRepository<ProxyDelegation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _approverId = EmployeeId.New();

    public DecideRequisitionApprovalCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _proxyDelegations.ListAsync(Arg.Any<ProxyDelegationsByDelegatorSpecification>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private (ApprovalChain Chain, JobRequisition Requisition) CreatePendingRequisitionWithChain()
    {
        var requisition = JobRequisition.Create(_tenantId, "Senior Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.SubmitForApproval(DateTimeOffset.UtcNow, "system");

        var chain = ApprovalChain.Create(_tenantId, ApprovalSubjectType.JobRequisition, requisition.Id.Value, [_approverId]).Value;

        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _approvalChains.FirstOrDefaultAsync(Arg.Any<ApprovalChainBySubjectSpecification>(), Arg.Any<CancellationToken>()).Returns(chain);

        return (chain, requisition);
    }

    private DecideRequisitionApprovalCommandHandler CreateHandler(EmployeeId signedInAs) => new(
        _approvalChains, _requisitionReads, _requisitions, _proxyDelegations, _tenantContext, _dateTimeProvider, _notificationDispatcher,
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, signedInAs));

    [Fact]
    public async Task Handle_Should_Approve_The_Requisition_When_The_Current_Approver_Approves()
    {
        var (_, requisition) = CreatePendingRequisitionWithChain();
        var handler = CreateHandler(_approverId);

        var result = await handler.Handle(
            new DecideRequisitionApprovalCommand(requisition.Id.Value, true, "Approved", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.Approved);
    }

    [Fact]
    public async Task Handle_Should_Reject_The_Requisition_When_The_Current_Approver_Rejects()
    {
        var (_, requisition) = CreatePendingRequisitionWithChain();
        var handler = CreateHandler(_approverId);

        var result = await handler.Handle(
            new DecideRequisitionApprovalCommand(requisition.Id.Value, false, "Headcount frozen", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.Rejected);
        requisition.RejectionReason.Should().Be("Headcount frozen");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Is_Not_The_Current_Approver()
    {
        var (_, requisition) = CreatePendingRequisitionWithChain();
        var handler = CreateHandler(EmployeeId.New());

        var result = await handler.Handle(new DecideRequisitionApprovalCommand(requisition.Id.Value, true, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        requisition.ApprovalStatus.Should().Be(RequisitionApprovalStatus.PendingApproval);
    }
}
