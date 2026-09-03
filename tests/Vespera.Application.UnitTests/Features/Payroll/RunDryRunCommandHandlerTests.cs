using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

/// <summary>Guards the batch-loading refactor in <see cref="RunDryRunCommandHandler"/>: given
/// multiple employees, every per-employee repository lookup (employee, LOP leave requests,
/// investment declarations, tax regime versions) must be issued exactly once for the whole run,
/// never once per employee — that was the N+1 this handler used to have.</summary>
public class RunDryRunCommandHandlerTests
{
    private static readonly DateOnly PeriodStart = new(2026, 5, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 5, 31);

    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<SalaryStructure> _salaryStructures = Substitute.For<IReadRepository<SalaryStructure>>();
    private readonly IReadRepository<SalaryComponent> _salaryComponents = Substitute.For<IReadRepository<SalaryComponent>>();
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets = Substitute.For<IReadRepository<StatutoryRuleSet>>();
    private readonly IReadRepository<InvestmentDeclaration> _investmentDeclarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly IReadRepository<TaxRegimeVersion> _taxRegimeVersions = Substitute.For<IReadRepository<TaxRegimeVersion>>();
    private readonly IReadRepository<LeaveRequest> _leaveRequests = Substitute.For<IReadRepository<LeaveRequest>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    [Fact]
    public async Task Handle_Should_Batch_Load_Every_Per_Employee_Dependency_Exactly_Once_Regardless_Of_Headcount()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var payrollRun = PayrollRun.Open(tenantId, 5, 2026, DateTimeOffset.UtcNow, "seed").Value;
        payrollRun.FreezeAttendance(new DateOnly(2026, 5, 1), freezeDay: 1, DateTimeOffset.UtcNow, "seed");
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<ISpecification<PayrollRun>>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var basicComponent = SalaryComponent.Create(tenantId, "Basic", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UtcNow, "seed").Value;
        _salaryComponents.ListAsync(Arg.Any<ISpecification<SalaryComponent>>(), Arg.Any<CancellationToken>())
            .Returns([basicComponent]);
        _statutoryRuleSets.ListAsync(Arg.Any<ISpecification<StatutoryRuleSet>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var employeeOne = Onboard(tenantId, "EMP-001");
        var employeeTwo = Onboard(tenantId, "EMP-002");

        var structureOne = Structure(tenantId, employeeOne.Id, basicComponent.Id);
        var structureTwo = Structure(tenantId, employeeTwo.Id, basicComponent.Id);
        _salaryStructures.ListAsync(Arg.Any<ISpecification<SalaryStructure>>(), Arg.Any<CancellationToken>())
            .Returns([structureOne, structureTwo]);

        // No LOP requests, no investment declarations for either employee — the simplest case that
        // still proves the batch call happens (once) rather than never happening at all.
        _leaveRequests.ListAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>()).Returns([]);
        _investmentDeclarations.ListAsync(Arg.Any<ISpecification<InvestmentDeclaration>>(), Arg.Any<CancellationToken>()).Returns([]);
        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>())
            .Returns([employeeOne, employeeTwo]);

        var handler = new RunDryRunCommandHandler(
            _payrollRuns, _salaryStructures, _salaryComponents, _statutoryRuleSets, _investmentDeclarations, _taxRegimeVersions,
            _leaveRequests, _employees, new PayrollComputationEngine([new EarningsRule(), new NetPayRule()]),
            _tenantContext, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(new RunDryRunCommand(payrollRun.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payrollRun.Lines.Should().HaveCount(2, "one line per employee with an active salary structure");

        // The actual regression guard: these must be called exactly once for the whole run, not
        // once per employee (2 employees here — a real N+1 would show up as 2 calls each).
        await _employees.Received(1).ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>());
        await _leaveRequests.Received(1).ListAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>());
        await _investmentDeclarations.Received(1).ListAsync(Arg.Any<ISpecification<InvestmentDeclaration>>(), Arg.Any<CancellationToken>());

        // No declarations exist for anyone, so the tax-regime batch query should be skipped
        // entirely rather than issued with an empty id list.
        await _taxRegimeVersions.DidNotReceive().ListAsync(Arg.Any<ISpecification<TaxRegimeVersion>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Skip_A_Salary_Structure_Whose_Employee_Record_Is_Not_In_The_Batch_Result()
    {
        var tenantId = TenantId.New();
        _tenantContext.TenantId.Returns(tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var payrollRun = PayrollRun.Open(tenantId, 5, 2026, DateTimeOffset.UtcNow, "seed").Value;
        payrollRun.FreezeAttendance(new DateOnly(2026, 5, 1), freezeDay: 1, DateTimeOffset.UtcNow, "seed");
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<ISpecification<PayrollRun>>(), Arg.Any<CancellationToken>()).Returns(payrollRun);

        var basicComponent = SalaryComponent.Create(tenantId, "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        _salaryComponents.ListAsync(Arg.Any<ISpecification<SalaryComponent>>(), Arg.Any<CancellationToken>()).Returns([basicComponent]);
        _statutoryRuleSets.ListAsync(Arg.Any<ISpecification<StatutoryRuleSet>>(), Arg.Any<CancellationToken>()).Returns([]);

        // A salary structure referencing an employee id the batch lookup does not return —
        // matches the original per-employee FirstOrDefaultAsync-returns-null-so-skip behavior.
        var structure = Structure(tenantId, EmployeeId.New(), basicComponent.Id);
        _salaryStructures.ListAsync(Arg.Any<ISpecification<SalaryStructure>>(), Arg.Any<CancellationToken>()).Returns([structure]);

        _leaveRequests.ListAsync(Arg.Any<ISpecification<LeaveRequest>>(), Arg.Any<CancellationToken>()).Returns([]);
        _investmentDeclarations.ListAsync(Arg.Any<ISpecification<InvestmentDeclaration>>(), Arg.Any<CancellationToken>()).Returns([]);
        _employees.ListAsync(Arg.Any<ISpecification<Employee>>(), Arg.Any<CancellationToken>()).Returns([]);

        var handler = new RunDryRunCommandHandler(
            _payrollRuns, _salaryStructures, _salaryComponents, _statutoryRuleSets, _investmentDeclarations, _taxRegimeVersions,
            _leaveRequests, _employees, new PayrollComputationEngine([new EarningsRule(), new NetPayRule()]),
            _tenantContext, _currentUser, _dateTimeProvider);

        var result = await handler.Handle(new RunDryRunCommand(payrollRun.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        payrollRun.Lines.Should().BeEmpty("the only salary structure's employee was not found, so it must be skipped, not error");
    }

    private static Employee Onboard(TenantId tenantId, string code) => Employee.Onboard(
        tenantId, EmployeeCode.Create(code).Value, "Test", "Employee",
        EmailAddress.Create($"{code.ToLowerInvariant()}@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
        new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
        DateTimeOffset.UtcNow, "seed").Value;

    private static SalaryStructure Structure(TenantId tenantId, EmployeeId employeeId, SalaryComponentId basicComponentId) =>
        SalaryStructure.Create(
            tenantId, employeeId, Money.Of(40000m, Currency.Inr),
            [SalaryStructureLine.Of(basicComponentId, SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr)))],
            PeriodStart, null).Value;
}
