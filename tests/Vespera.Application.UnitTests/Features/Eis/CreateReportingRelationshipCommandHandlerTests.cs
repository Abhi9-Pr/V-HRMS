using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class CreateReportingRelationshipCommandHandlerTests
{
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships = Substitute.For<IReadRepository<ReportingRelationship>>();
    private readonly IWriteRepository<ReportingRelationship> _reportingRelationshipWriter = Substitute.For<IWriteRepository<ReportingRelationship>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly List<ReportingRelationship> _seeded = [];

    public CreateReportingRelationshipCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _employees.AnyAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(true);

        _reportingRelationships.FirstOrDefaultAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Query(callInfo.Arg<ISpecification<ReportingRelationship>>()).FirstOrDefault());

        _reportingRelationships.ListAsync(Arg.Any<ISpecification<ReportingRelationship>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => (IReadOnlyList<ReportingRelationship>)Query(callInfo.Arg<ISpecification<ReportingRelationship>>()).ToList());
    }

    /// <summary>A minimal in-memory fake that actually applies each specification's real
    /// <c>Criteria</c> expression against the seeded rows — this is what lets the cycle-detection
    /// and overlap tests below exercise the handler's real chain-walk logic instead of a
    /// hand-wired stand-in for it.</summary>
    private IEnumerable<ReportingRelationship> Query(ISpecification<ReportingRelationship> specification) =>
        specification.Criteria is null ? _seeded : _seeded.Where(specification.Criteria.Compile());

    private CreateReportingRelationshipCommandHandler CreateHandler() =>
        new(_employees, _reportingRelationships, _reportingRelationshipWriter, _tenantContext);

    private void Seed(EmployeeId employeeId, EmployeeId managerId, DateOnly validFrom, DateOnly? validTo = null) =>
        _seeded.Add(ReportingRelationship.Create(_tenantId, employeeId, managerId, validFrom, validTo).Value);

    [Fact]
    public async Task Handle_Should_Reject_Direct_Self_Report()
    {
        var employeeId = EmployeeId.New();

        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(employeeId.Value, employeeId.Value, new DateOnly(2026, 1, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reporting_relationship.self_reporting");
    }

    [Fact]
    public async Task Handle_Should_Reject_An_Overlapping_Line_For_The_Same_Employee()
    {
        var employeeId = EmployeeId.New();
        Seed(employeeId, EmployeeId.New(), new DateOnly(2026, 1, 1));

        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(employeeId.Value, EmployeeId.New().Value, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("effective_dated.overlap");
    }

    [Fact]
    public async Task Handle_Should_Reject_A_ThreeHop_Cycle()
    {
        // Existing: employee2 reports to employee1; employee3 reports to employee2.
        var employee1 = EmployeeId.New();
        var employee2 = EmployeeId.New();
        var employee3 = EmployeeId.New();
        Seed(employee2, employee1, new DateOnly(2026, 1, 1));
        Seed(employee3, employee2, new DateOnly(2026, 1, 1));

        // Attempt: employee1 reports to employee3 -- closes the cycle 1 -> 3 -> 2 -> 1.
        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(employee1.Value, employee3.Value, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reporting_relationship.cycle_detected");
    }

    [Fact]
    public async Task Handle_Should_Reject_A_TwoHop_Cycle()
    {
        // Existing: employee2 reports to employee1.
        var employee1 = EmployeeId.New();
        var employee2 = EmployeeId.New();
        Seed(employee2, employee1, new DateOnly(2026, 1, 1));

        // Attempt: employee1 reports to employee2 -- direct A -> B -> A cycle.
        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(employee1.Value, employee2.Value, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("reporting_relationship.cycle_detected");
    }

    [Fact]
    public async Task Handle_Should_Accept_A_Legitimate_Deep_NonCyclic_Chain()
    {
        // Existing chain: e2 -> e1, e3 -> e2, e4 -> e3 (each reports to the next).
        var e1 = EmployeeId.New();
        var e2 = EmployeeId.New();
        var e3 = EmployeeId.New();
        var e4 = EmployeeId.New();
        Seed(e2, e1, new DateOnly(2026, 1, 1));
        Seed(e3, e2, new DateOnly(2026, 1, 1));
        Seed(e4, e3, new DateOnly(2026, 1, 1));

        // New employee e5 reports to e4 -- extends the chain, does not cycle.
        var e5 = EmployeeId.New();
        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(e5.Value, e4.Value, new DateOnly(2026, 6, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Employee_Missing()
    {
        _employees.AnyAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 1), null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("employee.not_found");
    }

    [Fact]
    public async Task Handle_Should_Persist_A_Valid_Relationship()
    {
        var employeeId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var result = await CreateHandler().Handle(
            new CreateReportingRelationshipCommand(employeeId, managerId, new DateOnly(2026, 1, 1), null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _reportingRelationshipWriter.Received(1).AddAsync(
            Arg.Is<ReportingRelationship>(rr => rr.EmployeeId.Value == employeeId && rr.ManagerId.Value == managerId),
            Arg.Any<CancellationToken>());
    }
}
