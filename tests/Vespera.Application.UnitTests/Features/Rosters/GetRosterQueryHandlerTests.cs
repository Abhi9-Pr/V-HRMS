using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Rosters;
using Vespera.Domain.Attendance;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Rosters;

public class GetRosterQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<ShiftRoster> _rosters = Substitute.For<IReadRepository<ShiftRoster>>();
    private readonly IReadRepository<Shift> _shifts = Substitute.For<IReadRepository<Shift>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();

    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public GetRosterQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
    }

    private GetRosterQueryHandler CreateHandler() => new(_employees, _rosters, _shifts, _tenantContext);

    private static Employee CreateEmployee() => Employee.Onboard(
        TenantId, EmployeeCode.Create("EMP-200").Value, "Grace", "Hopper",
        EmailAddress.Create("grace@vespera.test").Value, PhoneNumber.Create("+14155552672").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2026, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "seed").Value;

    [Fact]
    public async Task Handle_Should_Resolve_The_Published_Shift_For_Each_Day_In_Range()
    {
        var employee = CreateEmployee();
        var shift = Shift.Create(TenantId, "Day Shift", new TimeOnly(9, 0), new TimeOnly(18, 0), 10, Now, "hr@vespera.test").Value;
        var period = DateRange.Create(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 7)).Value;
        var roster = ShiftRoster.Create(TenantId, employee.Id, shift.Id, period, Now);
        roster.Publish(Now, "hr@vespera.test");

        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns([employee]);
        _rosters.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([roster]);
        _shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([shift]);

        var query = new GetRosterQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 3), null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var employeeRoster = result.Value.Employees.Single();
        employeeRoster.EmployeeId.Should().Be(employee.Id.Value);
        employeeRoster.Days.Should().HaveCount(3);
        employeeRoster.Days.Should().OnlyContain(day => day.ShiftId == shift.Id.Value && day.ShiftName == "Day Shift");
    }

    [Fact]
    public async Task Handle_Should_Return_Null_Shift_For_Unrostered_Days()
    {
        var employee = CreateEmployee();

        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns([employee]);
        _rosters.ListAsync(Arg.Any<ISpecification<ShiftRoster>>(), Arg.Any<CancellationToken>()).Returns([]);
        _shifts.ListAsync(Arg.Any<ISpecification<Shift>>(), Arg.Any<CancellationToken>()).Returns([]);

        var query = new GetRosterQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1), null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Employees.Single().Days.Single().ShiftId.Should().BeNull();
    }
}
