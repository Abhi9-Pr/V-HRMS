using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Workspace;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;
using Vespera.Domain.Workspace;

namespace Vespera.Application.UnitTests.Features.Workspace;

public class GetUpcomingCelebrationsQueryHandlerTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IReadRepository<Celebration> _celebrations = Substitute.For<IReadRepository<Celebration>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public GetUpcomingCelebrationsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId);
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    private GetUpcomingCelebrationsQueryHandler CreateHandler() => new(_celebrations, _employees, _tenantContext, _dateTimeProvider);

    private static Employee CreateEmployee(string firstName = "Alan", string lastName = "Turing") => Employee.Onboard(
        TenantId, EmployeeCode.Create($"EMP-{Guid.NewGuid():N}"[..12]).Value, firstName, lastName,
        EmailAddress.Create($"{Guid.NewGuid():N}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2020, 1, 1), DepartmentId.New(), DesignationId.New(), LocationId.New(), Now, "seed").Value;

    [Fact]
    public async Task Handle_Should_Include_A_Celebration_Within_The_Horizon()
    {
        var employee = CreateEmployee();
        var celebration = Celebration.Create(TenantId, employee.Id, CelebrationType.Birthday, new DateOnly(1990, 1, 15));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[employee]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(30), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(dto =>
            dto.EmployeeId == employee.Id.Value && dto.EmployeeName == "Alan Turing" && dto.NextOccurrence == new DateOnly(2026, 1, 15));
    }

    [Fact]
    public async Task Handle_Should_Exclude_A_Celebration_Beyond_The_Horizon()
    {
        var employee = CreateEmployee();
        var celebration = Celebration.Create(TenantId, employee.Id, CelebrationType.Birthday, new DateOnly(1990, 6, 15));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[employee]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(30), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Exclude_Employees_Who_Opted_Out_Of_Celebration_Visibility()
    {
        var employee = CreateEmployee();
        employee.SetCelebrationVisibility(false, Now, "hr@vespera.test");
        var celebration = Celebration.Create(TenantId, employee.Id, CelebrationType.Birthday, new DateOnly(1990, 1, 15));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[employee]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(30), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Exclude_A_Celebration_Whose_Employee_No_Longer_Exists()
    {
        var celebration = Celebration.Create(TenantId, EmployeeId.New(), CelebrationType.Birthday, new DateOnly(1990, 1, 15));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(30), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Roll_The_Occurrence_To_Next_Year_When_This_Years_Date_Has_Passed()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var employee = CreateEmployee();
        var celebration = Celebration.Create(TenantId, employee.Id, CelebrationType.WorkAnniversary, new DateOnly(1990, 3, 1));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[employee]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(400), CancellationToken.None);

        result.Value.Should().ContainSingle(dto => dto.NextOccurrence == new DateOnly(2027, 3, 1) && dto.YearsCount == 37);
    }

    [Fact]
    public async Task Handle_Should_Observe_A_Feb_29_Anniversary_On_Feb_28_In_A_Non_Leap_Year()
    {
        var employee = CreateEmployee();
        var celebration = Celebration.Create(TenantId, employee.Id, CelebrationType.Birthday, new DateOnly(2000, 2, 29));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[employee]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[celebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(90), CancellationToken.None);

        result.Value.Should().ContainSingle(dto => dto.NextOccurrence == new DateOnly(2026, 2, 28));
    }

    [Fact]
    public async Task Handle_Should_Order_Results_By_NextOccurrence_Ascending()
    {
        var later = CreateEmployee("Bob", "Later");
        var sooner = CreateEmployee("Amy", "Sooner");
        var laterCelebration = Celebration.Create(TenantId, later.Id, CelebrationType.Birthday, new DateOnly(1990, 1, 25));
        var soonerCelebration = Celebration.Create(TenantId, sooner.Id, CelebrationType.Birthday, new DateOnly(1990, 1, 10));
        _employees.ListAsync(Arg.Any<EmployeesByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)[later, sooner]);
        _celebrations.ListAsync(Arg.Any<CelebrationsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Celebration>)[laterCelebration, soonerCelebration]);

        var result = await CreateHandler().Handle(new GetUpcomingCelebrationsQuery(30), CancellationToken.None);

        result.Value.Should().HaveCount(2);
        result.Value[0].EmployeeId.Should().Be(sooner.Id.Value);
        result.Value[1].EmployeeId.Should().Be(later.Id.Value);
    }
}
