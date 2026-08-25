using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class DeleteEmployeeDocumentCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly TenantId _tenantId = TenantId.New();

    public DeleteEmployeeDocumentCommandHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private DeleteEmployeeDocumentCommandHandler CreateHandler() => new(_employees, _tenantContext, _fileStorage);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Remove_The_Document_And_Delete_Its_Blob()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "key-1", DateTimeOffset.UtcNow);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new DeleteEmployeeDocumentCommand(employee.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Documents.Should().BeEmpty();
        await _fileStorage.Received(1).DeleteAsync("key-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Document_Missing()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new DeleteEmployeeDocumentCommand(employee.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.not_found");
        await _fileStorage.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }
}
