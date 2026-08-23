using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class GetEmployeeDocumentsQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetEmployeeDocumentsQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetEmployeeDocumentsQueryHandler CreateHandler() => new(_employees, _tenantContext);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await CreateHandler().Handle(new GetEmployeeDocumentsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Documents_Newest_First()
    {
        var employee = CreateEmployee();
        employee.AddDocument(EmployeeDocumentType.Id, "key-1", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        employee.AddDocument(EmployeeDocumentType.Contract, "key-2", new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero));
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(new GetEmployeeDocumentsQuery(employee.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].DocumentType.Should().Be("Contract");
        result.Value[1].DocumentType.Should().Be("Id");
    }
}
