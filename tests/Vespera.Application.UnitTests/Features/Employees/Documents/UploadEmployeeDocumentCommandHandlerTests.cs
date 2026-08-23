using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Employees.Documents;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Documents;

public class UploadEmployeeDocumentCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IVirusScanner _virusScanner = Substitute.For<IVirusScanner>();
    private readonly TenantId _tenantId = TenantId.New();

    public UploadEmployeeDocumentCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UploadEmployeeDocumentCommandHandler CreateHandler() =>
        new(_employees, _tenantContext, _dateTimeProvider, _fileStorage, _virusScanner);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadEmployeeDocumentCommand(Guid.NewGuid(), EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Reject_An_Infected_File_Without_Uploading_Or_Attaching_It()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _virusScanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ScanResult.Infected);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadEmployeeDocumentCommand(employee.Id.Value, EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.infected");
        employee.Documents.Should().BeEmpty();
        await _fileStorage.DidNotReceiveWithAnyArgs().UploadAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_Should_Attach_A_Clean_Document_Marked_Clean()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _virusScanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ScanResult.Clean);
        _fileStorage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("storage-key-1");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadEmployeeDocumentCommand(employee.Id.Value, EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Documents.Should().ContainSingle();
        var document = employee.Documents.Single();
        document.ScanStatus.Should().Be(DocumentScanStatus.Clean);
        document.FileReference.Should().Be("storage-key-1");
    }

    [Fact]
    public async Task Handle_Should_Attach_A_Document_Marked_Failed_When_Scan_Fails()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _virusScanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(ScanResult.ScanFailed);
        _fileStorage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("storage-key-2");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadEmployeeDocumentCommand(employee.Id.Value, EmployeeDocumentType.Id, "id.jpg", [1, 2, 3]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Documents.Single().ScanStatus.Should().Be(DocumentScanStatus.Failed);
    }
}
