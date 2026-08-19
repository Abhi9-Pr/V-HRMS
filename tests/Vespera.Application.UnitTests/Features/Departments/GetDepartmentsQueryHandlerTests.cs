using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class GetDepartmentsQueryHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    [Fact]
    public async Task Handle_Should_Return_Mapped_Paged_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);

        var department = Department.Create(tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "system").Value;

        _departments.ListAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Department> { department });
        _departments.CountAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var handler = new GetDepartmentsQueryHandler(_departments, _tenantContext);
        var query = new GetDepartmentsQuery(new PagedRequest());

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().ContainSingle(dto =>
            dto.Name == "Engineering" && dto.Code == "ENG" && dto.Id == department.Id.Value);
    }
}
