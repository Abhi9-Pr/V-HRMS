using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Employees;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Employees;

public class GetEmployeeByIdQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetEmployeeByIdQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetEmployeeByIdQueryHandler CreateHandler() => new(_employees, _tenantContext);

    private Employee CreateEmployee() => Employee.Onboard(
        _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
        EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    [Fact]
    public async Task Handle_Should_Return_Dto_When_Employee_Exists()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEmployeeByIdQuery(employee.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Ada");
        result.Value.DepartmentId.Should().Be(employee.DepartmentId.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEmployeeByIdQuery(Guid.NewGuid(), null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Always_Mask_Pan_And_BankAccount()
    {
        var employee = CreateEmployee();
        employee.UpdateStatutoryDetails(
            PanNumber.Create("ABCDE1234F").Value, BankAccountNumber.Create("123456789012").Value, DateTimeOffset.UtcNow, "seed");
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEmployeeByIdQuery(employee.Id.Value, null), CancellationToken.None);

        result.Value.MaskedPan.Should().NotBe("ABCDE1234F");
        result.Value.MaskedPan.Should().Contain("*");
        result.Value.MaskedBankAccount.Should().NotBe("123456789012");
        result.Value.MaskedBankAccount.Should().Contain("*");
    }

    [Fact]
    public async Task Handle_Should_Resolve_The_Department_As_Of_A_Past_Transfer_Date()
    {
        var employee = CreateEmployee();
        var originalDepartmentId = employee.DepartmentId;
        var newDepartmentId = DepartmentId.New();
        employee.Transfer(
            newDepartmentId, DesignationId.New(), LocationId.New(),
            new DateOnly(2025, 6, 1), EmploymentChangeReason.Transfer, DateTimeOffset.UtcNow, "seed");
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetEmployeeByIdQuery(employee.Id.Value, new DateOnly(2024, 6, 1)), CancellationToken.None);

        result.Value.DepartmentId.Should().Be(originalDepartmentId.Value);
    }
}
