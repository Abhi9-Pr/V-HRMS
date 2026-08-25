using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.OrgChart;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.OrgChart;

public class GetOrgChartQueryHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IReadRepository<Department> _departments = Substitute.For<IReadRepository<Department>>();
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly List<ReportingRelationship> _seededRelationships = [];

    public GetOrgChartQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);

        _departments.ListAsync(Arg.Any<ISpecification<Department>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Department>)[]);
        _designations.ListAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Designation>)[]);

        // Applies the real ActiveReportingRelationshipsSpecification.Criteria against the seeded
        // rows, so the AsOf-filtering tests below exercise the actual as-of logic, not a stand-in.
        _reportingRelationships.ListAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var specification = callInfo.Arg<ISpecification<ReportingRelationship>>();
                IEnumerable<ReportingRelationship> query = specification.Criteria is null
                    ? _seededRelationships
                    : _seededRelationships.Where(specification.Criteria.Compile());
                return (IReadOnlyList<ReportingRelationship>)query.ToList();
            });
    }

    private GetOrgChartQueryHandler CreateHandler() => new(_employees, _reportingRelationships, _departments, _designations, _tenantContext);

    private Employee CreateEmployee(string firstName, string lastName) => Employee.Onboard(
        _tenantId, EmployeeCode.Create($"EMP-{Guid.NewGuid():N}"[..12]).Value, firstName, lastName,
        EmailAddress.Create($"{Guid.NewGuid():N}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15),
        DepartmentId.New(), DesignationId.New(), LocationId.New(), DateTimeOffset.UtcNow, "seed").Value;

    private void SetEmployees(params Employee[] employees) =>
        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<Employee>)employees.ToList());

    private void Seed(EmployeeId employeeId, EmployeeId managerId, DateOnly validFrom, DateOnly? validTo = null) =>
        _seededRelationships.Add(ReportingRelationship.Create(_tenantId, employeeId, managerId, validFrom, validTo).Value);

    [Fact]
    public async Task Handle_Should_Return_Multiple_Independent_Roots_With_Nested_Subordinates_And_Leaves()
    {
        var ceo1 = CreateEmployee("Ada", "Lovelace");
        var report1 = CreateEmployee("Grace", "Hopper");
        var ceo2 = CreateEmployee("Alan", "Turing");

        SetEmployees(ceo1, report1, ceo2);
        Seed(report1.Id, ceo1.Id, new DateOnly(2026, 1, 1));

        var result = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 6, 1), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Roots.Should().HaveCount(2);

        var ceo1Node = result.Value.Roots.Should().ContainSingle(n => n.EmployeeId == ceo1.Id.Value).Subject;
        ceo1Node.Children.Should().ContainSingle(c => c.EmployeeId == report1.Id.Value);
        ceo1Node.Children.Single().Children.Should().BeEmpty();

        var ceo2Node = result.Value.Roots.Should().ContainSingle(n => n.EmployeeId == ceo2.Id.Value).Subject;
        ceo2Node.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_Exclude_A_Relationship_That_Starts_After_AsOf()
    {
        var manager = CreateEmployee("Ada", "Lovelace");
        var report = CreateEmployee("Grace", "Hopper");
        SetEmployees(manager, report);
        Seed(report.Id, manager.Id, new DateOnly(2026, 6, 1));

        var result = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 1, 1), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // The relationship isn't active yet, so both employees are roots with no children.
        result.Value.Roots.Should().HaveCount(2);
        result.Value.Roots.Should().OnlyContain(n => n.Children.Count == 0);
    }

    [Fact]
    public async Task Handle_Should_Include_A_Closed_Relationship_That_Was_Active_On_AsOf()
    {
        var manager = CreateEmployee("Ada", "Lovelace");
        var report = CreateEmployee("Grace", "Hopper");
        SetEmployees(manager, report);
        Seed(report.Id, manager.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        var result = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 6, 1), null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Roots.Should().ContainSingle(n => n.EmployeeId == manager.Id.Value)
            .Subject.Children.Should().ContainSingle(c => c.EmployeeId == report.Id.Value);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_RootEmployeeId_Does_Not_Exist()
    {
        SetEmployees();

        var result = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 6, 1), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Produce_The_Same_ETag_For_The_Same_Underlying_State()
    {
        var manager = CreateEmployee("Ada", "Lovelace");
        var report = CreateEmployee("Grace", "Hopper");
        SetEmployees(manager, report);
        Seed(report.Id, manager.Id, new DateOnly(2026, 1, 1));

        var first = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 6, 1), null), CancellationToken.None);
        var second = await CreateHandler().Handle(new GetOrgChartQuery(new DateOnly(2026, 6, 1), null), CancellationToken.None);

        first.Value.ETag.Should().Be(second.Value.ETag);
    }
}
