using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees;

public class TransferEmployeeCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public TransferEmployeeCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private TransferEmployeeCommandHandler CreateHandler() => new(_employees, _tenantContext, _currentUser, _dateTimeProvider);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Transfer_The_Employee_When_Valid()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);
        var newDepartmentId = Guid.NewGuid();

        var handler = CreateHandler();
        var command = new TransferEmployeeCommand(
            employee.Id.Value, newDepartmentId, Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), "Transfer");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.DepartmentId.Should().Be(new DepartmentId(newDepartmentId));
        employee.EmploymentHistory.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var command = new TransferEmployeeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), "Transfer");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Reason_Is_Unrecognized()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var command = new TransferEmployeeCommand(
            employee.Id.Value, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), "NotAReason");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.invalid_change_reason");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Employee_Already_Exited()
    {
        var employee = CreateEmployee();
        employee.Exit(new DateOnly(2026, 2, 1), EmployeeExitReason.Resignation, DateTimeOffset.UtcNow, "seed");
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var command = new TransferEmployeeCommand(
            employee.Id.Value, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 3, 1), "Transfer");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.exited");
    }
}
