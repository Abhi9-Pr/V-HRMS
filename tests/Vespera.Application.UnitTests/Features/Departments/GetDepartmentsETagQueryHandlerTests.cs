using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Departments;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Departments;

public class GetDepartmentsETagQueryHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetDepartmentsETagQueryHandler CreateHandler() => new(_departments, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_The_Same_Hash_For_The_Same_Departments()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var department = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "system").Value;
        _departments.ListAsync(Arg.Any<DepartmentsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([department]);

        var first = await CreateHandler().Handle(new GetDepartmentsETagQuery(), CancellationToken.None);
        var second = await CreateHandler().Handle(new GetDepartmentsETagQuery(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value.Should().NotBeNullOrEmpty();
        second.Value.Should().Be(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Different_Hash_When_The_Department_Set_Differs()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var departmentA = Department.Create(_tenantId, "Engineering", "ENG", null, DateTimeOffset.UtcNow, "system").Value;
        var departmentB = Department.Create(_tenantId, "Sales", "SLS", null, DateTimeOffset.UtcNow, "system").Value;

        _departments.ListAsync(Arg.Any<DepartmentsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([departmentA]);
        var first = await CreateHandler().Handle(new GetDepartmentsETagQuery(), CancellationToken.None);

        _departments.ListAsync(Arg.Any<DepartmentsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([departmentA, departmentB]);
        var second = await CreateHandler().Handle(new GetDepartmentsETagQuery(), CancellationToken.None);

        second.Value.Should().NotBe(first.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Hash_Even_When_There_Are_No_Departments()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _departments.ListAsync(Arg.Any<DepartmentsByTenantSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetDepartmentsETagQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
    }
}
