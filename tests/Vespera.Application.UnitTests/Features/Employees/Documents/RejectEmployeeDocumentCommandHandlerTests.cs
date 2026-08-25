using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class RejectEmployeeDocumentCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public RejectEmployeeDocumentCommandHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private RejectEmployeeDocumentCommandHandler CreateHandler() => new(_employees, _tenantContext);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Reject_The_Document_With_A_Reason()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "key-1", DateTimeOffset.UtcNow);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new RejectEmployeeDocumentCommand(employee.Id.Value, document.Id.Value, "Blurry scan"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        document.VerificationStatus.Should().Be(DocumentVerificationStatus.Rejected);
        document.RejectionReason.Should().Be("Blurry scan");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await CreateHandler().Handle(
            new RejectEmployeeDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), "Blurry scan"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }
}
