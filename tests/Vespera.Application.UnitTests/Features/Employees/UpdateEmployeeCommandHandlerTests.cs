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

public class UpdateEmployeeCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateEmployeeCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateEmployeeCommandHandler CreateHandler() => new(_employees, _tenantContext, _currentUser, _dateTimeProvider);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Update_Personal_Statutory_And_Compensation_Details()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var command = new UpdateEmployeeCommand(
            employee.Id.Value, "Augusta", "King", "augusta.king@vespera.test", "+442071234567",
            "ABCDE1234F", "123456789012", 1200000m, "Inr");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.FirstName.Should().Be("Augusta");
        employee.LastName.Should().Be("King");
        employee.Pan!.Value.Should().Be("ABCDE1234F");
        employee.BankAccount!.Value.Should().Be("123456789012");
        employee.CurrentAnnualCtc.Should().Be(Money.Of(1200000m, Currency.Inr));
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var command = new UpdateEmployeeCommand(Guid.NewGuid(), "Ada", "Lovelace", "ada@vespera.test", "+14155552671", null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Currency_Is_Unrecognized()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var command = new UpdateEmployeeCommand(
            employee.Id.Value, "Ada", "Lovelace", "ada@vespera.test", "+14155552671", null, null, 1200000m, "NotACurrency");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.invalid_currency");
    }

    [Fact]
    public async Task Handle_Should_Allow_Clearing_Statutory_And_Compensation_Details()
    {
        var employee = CreateEmployee();
        employee.UpdateStatutoryDetails(
            PanNumber.Create("ABCDE1234F").Value, BankAccountNumber.Create("123456789012").Value, DateTimeOffset.UtcNow, "seed");
        employee.UpdateCompensation(Money.Of(1200000m, Currency.Inr), DateTimeOffset.UtcNow, "seed");
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var command = new UpdateEmployeeCommand(employee.Id.Value, "Ada", "Lovelace", "ada@vespera.test", "+14155552671", null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        employee.Pan.Should().BeNull();
        employee.BankAccount.Should().BeNull();
        employee.CurrentAnnualCtc.Should().BeNull();
    }
}
