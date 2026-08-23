using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class GetDepartmentByIdQueryHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetDepartmentByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetDepartmentByIdQueryHandler CreateHandler() => new(_departments, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_Dto_When_Department_Exists()
    {
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "seed").Value;
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns(department);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDepartmentByIdQuery(department.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Engineering");
        result.Value.Code.Should().Be("ENG");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Department_Missing()
    {
        _departments.FirstOrDefaultAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>()).Returns((Department?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetDepartmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("department.not_found");
    }
}
