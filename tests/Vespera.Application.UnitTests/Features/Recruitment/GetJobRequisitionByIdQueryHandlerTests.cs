using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetJobRequisitionByIdQueryHandlerTests
{
    private readonly IReadRepository<JobRequisition> _requisitions = Substitute.For<IReadRepository<JobRequisition>>();

    private GetJobRequisitionByIdQueryHandler CreateHandler() => new(_requisitions);

    [Fact]
    public async Task Handle_Should_Return_The_Requisition_Dto_With_Its_Stages()
    {
        var requisition = JobRequisition.Create(TenantId.New(), "Engineer", DepartmentId.New(), 2, DateTimeOffset.UtcNow, "system").Value;
        requisition.AddStage("Screening", DateTimeOffset.UtcNow, "system");
        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(requisition);

        var result = await CreateHandler().Handle(new GetJobRequisitionByIdQuery(requisition.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(requisition.Id.Value);
        result.Value.Title.Should().Be("Engineer");
        result.Value.Stages.Should().ContainSingle(s => s.Name == "Screening");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Requisition_Not_Found()
    {
        _requisitions.FirstOrDefaultAsync(Arg.Any<JobRequisitionByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((JobRequisition?)null);

        var result = await CreateHandler().Handle(new GetJobRequisitionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
