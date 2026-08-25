using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees;

public class GetEmployeesQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetEmployeesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetEmployeesQueryHandler CreateHandler() => new(_employees, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Employee_Summaries()
    {
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Employee> { employee });
        _employees.CountAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEmployeesQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items[0].FullName.Should().Be("Ada Lovelace");
        result.Value.TotalCount.Should().Be(1);
    }
}
