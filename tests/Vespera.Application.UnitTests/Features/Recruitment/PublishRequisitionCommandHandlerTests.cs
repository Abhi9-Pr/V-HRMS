using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class PublishRequisitionCommandHandlerTests
{
    private readonly IReadRepository<JobRequisition> _requisitionReads = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly IWriteRepository<JobRequisition> _requisitions = Substitute.For<IWriteRepository<JobRequisition>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private PublishRequisitionCommandHandler CreateHandler() => new(_requisitionReads, _requisitions, _currentUser, _dateTimeProvider);

    private JobRequisition CreateApprovedRequisition()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        requisition.SubmitForApproval(DateTimeOffset.UtcNow, "system");
        requisition.ApproveRequisition(DateTimeOffset.UtcNow, "system");
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        return requisition;
    }

    [Fact]
    public async Task Handle_Should_Publish_An_Approved_Open_Requisition()
    {
        var requisition = CreateApprovedRequisition();
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new PublishRequisitionCommand(requisition.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        requisition.IsPublished.Should().BeTrue();
        _requisitions.Received(1).Update(requisition);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Requisition_Not_Found()
    {
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((JobRequisition?)null);

        var result = await CreateHandler().Handle(new PublishRequisitionCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Requisition_Is_Not_Yet_Approved()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new PublishRequisitionCommand(requisition.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _requisitions.DidNotReceive().Update(Arg.Any<JobRequisition>());
    }
}
