using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Recruitment;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.UnitTests.Features.Recruitment;

public class GetPublicJobsQueryHandlerTests
{
    private readonly IVesperaDbContext _dbContext = Substitute.For<IVesperaDbContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetPublicJobsQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetPublicJobsQueryHandler CreateHandler() => new(_dbContext, _tenantContext);

    private JobRequisition CreatePublishedRequisition(DepartmentId departmentId)
    {
        var requisition = JobRequisition.Create(_tenantId, "Senior Engineer", departmentId, 3, DateTimeOffset.UtcNow, "system").Value;
        requisition.SubmitForApproval(DateTimeOffset.UtcNow, "system");
        requisition.ApproveRequisition(DateTimeOffset.UtcNow, "system");
        requisition.Publish(DateTimeOffset.UtcNow, "system");
        return requisition;
    }

    [Fact]
    public async Task Handle_Should_Return_Only_Published_Open_Approved_Requisitions()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "system").Value;
        var publishedRequisition = CreatePublishedRequisition(department.Id);
        var draftRequisition = JobRequisition.Create(_tenantId, "Unpublished Role", department.Id, 1, DateTimeOffset.UtcNow, "system").Value;

        _dbContext.Set<JobRequisition>().Returns(new List<JobRequisition> { publishedRequisition, draftRequisition }.AsQueryable());
        _dbContext.Set<Department>().Returns(new List<Department> { department }.AsQueryable());

        var result = await CreateHandler().Handle(new GetPublicJobsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(job =>
            job.Id == publishedRequisition.Id.Value && job.Title == "Senior Engineer" && job.DepartmentName == "Engineering"
            && job.OpeningsCount == 3);
    }

    [Fact]
    public async Task Handle_Should_Return_An_Empty_List_When_There_Are_No_Published_Requisitions()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "system").Value;
        var draftRequisition = JobRequisition.Create(_tenantId, "Unpublished Role", department.Id, 1, DateTimeOffset.UtcNow, "system").Value;

        _dbContext.Set<JobRequisition>().Returns(new List<JobRequisition> { draftRequisition }.AsQueryable());
        _dbContext.Set<Department>().Returns(new List<Department> { department }.AsQueryable());

        var result = await CreateHandler().Handle(new GetPublicJobsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
