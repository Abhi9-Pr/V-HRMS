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

public class GetEmployeeDocumentDownloadUrlQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetEmployeeDocumentDownloadUrlQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private GetEmployeeDocumentDownloadUrlQueryHandler CreateHandler() =>
        new(_employees, _tenantContext, _dateTimeProvider, _fileStorage);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Document_Missing()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new GetEmployeeDocumentDownloadUrlQuery(employee.Id.Value, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Document_Has_Not_Cleared_Scan()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "key-1", DateTimeOffset.UtcNow);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(
            new GetEmployeeDocumentDownloadUrlQuery(employee.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee_document.not_clean");
    }

    [Fact]
    public async Task Handle_Should_Return_A_Signed_Url_For_A_Clean_Document()
    {
        var employee = CreateEmployee();
        var document = employee.AddDocument(EmployeeDocumentType.Id, "key-1", DateTimeOffset.UtcNow);
        document.MarkScanned(DocumentScanStatus.Clean);
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        _fileStorage.GetDownloadUrlAsync("key-1", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new Uri("https://files.vespera.test/key-1?sig=abc"));

        var result = await CreateHandler().Handle(
            new GetEmployeeDocumentDownloadUrlQuery(employee.Id.Value, document.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Url.Should().Be("https://files.vespera.test/key-1?sig=abc");
    }
}
