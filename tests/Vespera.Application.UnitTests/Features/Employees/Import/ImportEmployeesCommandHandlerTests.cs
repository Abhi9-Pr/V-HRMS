using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees.Import;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees.Import;

public class ImportEmployeesCommandHandlerTests
{
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly IReadRepository<Location> _locations = Substitute.For<IReadRepository<Location>>();
    private readonly IReadRepository<Employee> _existingEmployees = Substitute.For<IReadRepository<Employee>>();
    private readonly IWriteRepository<Employee> _employeeWriter = Substitute.For<IWriteRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private readonly Department _department = Department.Create(
        TenantId.New(), "Engineering", "ENG", null, DateTimeOffset.UtcNow, "seed").Value;
    private readonly Designation _designation = Designation.Create(
        TenantId.New(), "Software Engineer", 3, DateTimeOffset.UtcNow, "seed").Value;
    private readonly Location _location = Location.Create(
        TenantId.New(), "HQ", "1 Main St", "Pune", "India",
        GeoCoordinate.Create(18.5204, 73.8567).Value, "Asia/Kolkata", DateTimeOffset.UtcNow, "seed").Value;

    public ImportEmployeesCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        _departments.ListAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>())
            .Returns([_department]);
        _designations.ListAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>())
            .Returns([_designation]);
        _locations.ListAsync(Arg.Any<ISpecification<Location>>(), Arg.Any<CancellationToken>())
            .Returns([_location]);
        _existingEmployees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    private ImportEmployeesCommandHandler CreateHandler() => new(
        _departments, _designations, _locations, _existingEmployees, _employeeWriter, _tenantContext, _currentUser, _dateTimeProvider);

    private ImportEmployeeRowDto ValidRow(int rowNumber = 1, string code = "EMP-100") => new(
        rowNumber, code, "Grace", "Hopper", $"grace.hopper.{code}@vespera.test", "+14155552672",
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        _department.Code, _designation.Title, _location.Name);

    private ImportEmployeeRowDto InvalidRow(int rowNumber = 1) => ValidRow(rowNumber) with { DepartmentCode = "does-not-exist" };

    [Fact]
    public async Task Handle_DryRun_With_Valid_Rows_Should_Report_Success_And_Persist_Nothing()
    {
        var handler = CreateHandler();
        var command = new ImportEmployeesCommand([ValidRow()], DryRun: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Committed.Should().BeFalse();
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(0);
        await _employeeWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_DryRun_With_Invalid_Rows_Should_Report_Row_Level_Errors()
    {
        var handler = CreateHandler();
        var command = new ImportEmployeesCommand([InvalidRow()], DryRun: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Committed.Should().BeFalse();
        result.Value.FailureCount.Should().Be(1);
        result.Value.Rows.Single().Errors.Should().Contain(e => e.Contains("does-not-exist"));
        await _employeeWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Real_Run_With_All_Valid_Rows_Should_Create_Every_Employee_And_Commit()
    {
        var handler = CreateHandler();
        var command = new ImportEmployeesCommand(
            [ValidRow(1, "EMP-100"), ValidRow(2, "EMP-101")], DryRun: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Committed.Should().BeTrue();
        result.Value.SuccessCount.Should().Be(2);
        await _employeeWriter.Received(2).AddAsync(Arg.Any<Employee>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Real_Run_With_A_Mix_Of_Valid_And_Invalid_Rows_Should_Create_Nothing()
    {
        var handler = CreateHandler();
        var command = new ImportEmployeesCommand([ValidRow(1, "EMP-100"), InvalidRow(2)], DryRun: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Committed.Should().BeFalse();
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailureCount.Should().Be(1);
        await _employeeWriter.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Reject_Duplicate_Employee_Codes_Within_The_Same_Batch()
    {
        var handler = CreateHandler();
        var command = new ImportEmployeesCommand(
            [ValidRow(1, "EMP-100"), ValidRow(2, "EMP-100")], DryRun: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Value.FailureCount.Should().Be(1);
        result.Value.Rows[^1].Errors.Should().Contain(e => e.Contains("already in use"));
    }
}
