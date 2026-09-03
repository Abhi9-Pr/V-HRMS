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

public class AddPipelineStageCommandHandlerTests
{
    private readonly IWriteRepository<JobRequisition> _requisitions = Substitute.For<IWriteRepository<JobRequisition>>();
    private readonly IReadRepository<JobRequisition> _requisitionReads = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private AddPipelineStageCommandHandler CreateHandler() => new(_requisitions, _requisitionReads, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_A_Stage_And_Update_The_Requisition()
    {
        var requisition = JobRequisition.Create(TenantId.New(), "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new AddPipelineStageCommand(requisition.Id.Value, "Screening", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        requisition.Stages.Should().ContainSingle(s => s.Name == "Screening");
        _requisitions.Received(1).Update(requisition);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Requisition_Not_Found()
    {
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((JobRequisition?)null);

        var result = await CreateHandler().Handle(new AddPipelineStageCommand(Guid.NewGuid(), "Screening", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _requisitions.DidNotReceive().Update(Arg.Any<JobRequisition>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Stage_Name_Is_Blank()
    {
        var requisition = JobRequisition.Create(TenantId.New(), "Engineer", DepartmentId.New(), 1, DateTimeOffset.UtcNow, "system").Value;
        _requisitionReads.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new AddPipelineStageCommand(requisition.Id.Value, "   ", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _requisitions.DidNotReceive().Update(Arg.Any<JobRequisition>());
    }
}
