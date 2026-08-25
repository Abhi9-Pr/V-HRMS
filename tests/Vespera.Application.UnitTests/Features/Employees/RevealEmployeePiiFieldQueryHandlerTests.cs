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

public class RevealEmployeePiiFieldQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    public RevealEmployeePiiFieldQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private RevealEmployeePiiFieldQueryHandler CreateHandler() =>
        new(_employees, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    private Employee CreateEmployee()
    {
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-100").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15),
            DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;
        employee.UpdateStatutoryDetails(PanNumber.Create("ABCDE1234F").Value, null, DateTimeOffset.UtcNow, "seed");
        return employee;
    }

    [Fact]
    public async Task Handle_Should_Return_The_Real_Value_And_Record_An_Access_Entry()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var result = await handler.Handle(new RevealEmployeePiiFieldQuery(employee.Id.Value, EmployeePiiField.Pan), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ABCDE1234F");
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "Employee", employee.Id.Value, "Pan", Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_The_Field_Has_No_Value_On_Record()
    {
        var employee = CreateEmployee();
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(employee);

        var handler = CreateHandler();
        var result = await handler.Handle(new RevealEmployeePiiFieldQuery(employee.Id.Value, EmployeePiiField.BankAccount), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.pii_field_not_set");
        await _piiAccessAuditor.DidNotReceiveWithAnyArgs().RecordAccessAsync(
            default, default!, default, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.FirstOrDefaultAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new RevealEmployeePiiFieldQuery(Guid.NewGuid(), EmployeePiiField.Pan), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("employee.not_found");
    }
}
