using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class VerifyEmployeeDocumentCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public VerifyEmployeeDocumentCommandHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private VerifyEmployeeDocumentCommandHandler CreateHandler() => new(_employees, _tenantContext);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Verify_The_Document()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "key-1", DateTimeOffset.UtcNow);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new VerifyEmployeeDocumentCommand(employee.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.VerificationStatus.Should().Be(DocumentVerificationStatus.Verified);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Document_Missing()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new VerifyEmployeeDocumentCommand(employee.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.not_found");
    }
}
