using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetJobRequisitionsQueryHandlerTests
{
    private readonly IReadRepository<JobRequisition> _requisitions = Substitute.For<IReadRepository<JobRequisition>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetJobRequisitionsQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetJobRequisitionsQueryHandler CreateHandler() => new(_requisitions, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Requisitions()
    {
        var requisition = JobRequisition.Create(_tenantId, "Engineer", DepartmentId.New(), 2, DateTimeOffset.UtcNow, "system").Value;

        _requisitions.ListAsync(Arg.Any<JobRequisitionsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([requisition]);
        _requisitions.CountAsync(Arg.Any<JobRequisitionsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new GetJobRequisitionsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == requisition.Id.Value && dto.Title == "Engineer");
        result.Value.TotalCount.Should().Be(1);
    }
}
