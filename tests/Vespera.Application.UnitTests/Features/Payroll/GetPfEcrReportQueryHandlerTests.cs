using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Employees;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetPfEcrReportQueryHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<StatutoryRuleSet> _statutoryRuleSets = Substitute.For<IReadRepository<StatutoryRuleSet>>();
    private readonly IReadRepository<Employee> _employees = Substitute.For<IReadRepository<Employee>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetPfEcrReportQueryHandler CreateHandler() =>
        new(_payrollRuns, _statutoryRuleSets, _employees, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    private PayrollRun RunWithOneLine(EmployeeId employeeId, DateTimeOffset now)
    {
        var run = PayrollRun.Open(_tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        run.RecomputeLines(
            [new PayrollLineInput(employeeId, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        return run;
    }

    [Fact]
    public async Task Handle_Should_Compute_Employee_Contribution_At_The_Configured_Rate_With_No_Cap()
    {
        var now = DateTimeOffset.UtcNow;
        var employee = Employee.Onboard(
            _tenantId, EmployeeCode.Create("EMP-300").Value, "Ada", "Lovelace",
            EmailAddress.Create("ada@vespera.test").Value, PhoneNumber.Create("+14155552671").Value,
            new DateOnly(1990, 1, 1), new DateOnly(2024, 1, 15), DepartmentId.New(), DesignationId.New(), LocationId.New(),
            DateTimeOffset.UtcNow, "seed").Value;
        var run = RunWithOneLine(employee.Id, now);
        var pfRule = StatutoryRuleSet.Create(_tenantId, StatutoryRuleType.ProvidentFund, 12m, null, new DateOnly(2026, 1, 1), null).Value;

        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _statutoryRuleSets.ListAsync(Arg.Any<StatutoryRuleSetsActiveOnDateSpecification>(), Arg.Any<CancellationToken>()).Returns([pfRule]);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(employee);

        var result = await CreateHandler().Handle(new GetPfEcrReportQuery(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value[0].EmployeeContribution.Should().Be(4800m);
        result.Value[0].EmployeeName.Should().Be("Ada Lovelace");
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "PayrollRun", run.Id.Value, "PfEcrReport", "system", now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Cap_The_Contribution_At_The_Configured_Cap_Amount()
    {
        var now = DateTimeOffset.UtcNow;
        var employeeId = EmployeeId.New();
        var run = RunWithOneLine(employeeId, now);
        var pfRule = StatutoryRuleSet.Create(
            _tenantId, StatutoryRuleType.ProvidentFund, 12m, Money.Of(1800m, Currency.Inr), new DateOnly(2026, 1, 1), null).Value;

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _statutoryRuleSets.ListAsync(Arg.Any<StatutoryRuleSetsActiveOnDateSpecification>(), Arg.Any<CancellationToken>()).Returns([pfRule]);
        _employees.FirstOrDefaultAsync(Arg.Any<EmployeeByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Employee?)null);

        var result = await CreateHandler().Handle(new GetPfEcrReportQuery(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].EmployeeContribution.Should().Be(1800m);
        result.Value[0].EmployeeName.Should().Be("Unknown");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Provident_Fund_Rule_Is_Configured()
    {
        var now = DateTimeOffset.UtcNow;
        var run = RunWithOneLine(EmployeeId.New(), now);

        _tenantContext.TenantId.Returns(_tenantId);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _statutoryRuleSets.ListAsync(Arg.Any<StatutoryRuleSetsActiveOnDateSpecification>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetPfEcrReportQuery(run.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("pf_ecr_report.no_rule");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new GetPfEcrReportQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
