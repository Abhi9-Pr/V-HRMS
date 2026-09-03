using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetPayrollRunVarianceQueryHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<PayrollSettings> _payrollSettings = Substitute.For<IReadRepository<PayrollSettings>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetPayrollRunVarianceQueryHandler CreateHandler() =>
        new(_payrollRuns, _payrollSettings, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    private static PayrollRun RunWithLines(TenantId tenantId, DateTimeOffset now, params PayrollLineInput[] lines)
    {
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), 25, now, "seed");
        run.RecomputeLines(lines, "seed", now);
        return run;
    }

    [Fact]
    public async Task Handle_Should_Flag_LargeVariance_NewJoiner_Exit_And_ZeroNet_Against_The_Prior_Run()
    {
        var now = DateTimeOffset.UtcNow;
        var stableEmployee = EmployeeId.New();
        var largeVarianceEmployee = EmployeeId.New();
        var newJoiner = EmployeeId.New();
        var exitedEmployee = EmployeeId.New();
        var zeroNetEmployee = EmployeeId.New();

        var priorRun = RunWithLines(_tenantId, now,
            new PayrollLineInput(stableEmployee, Money.Of(50000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(50000m, Currency.Inr), 0),
            new PayrollLineInput(largeVarianceEmployee, Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0),
            new PayrollLineInput(exitedEmployee, Money.Of(30000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(30000m, Currency.Inr), 0));
        priorRun.SubmitForReview();
        priorRun.Approve("seed", now);
        priorRun.Finalize(now);

        var currentRun = RunWithLines(_tenantId, now,
            new PayrollLineInput(stableEmployee, Money.Of(50500m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(50500m, Currency.Inr), 0),
            new PayrollLineInput(largeVarianceEmployee, Money.Of(90000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(90000m, Currency.Inr), 0),
            new PayrollLineInput(newJoiner, Money.Of(20000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(20000m, Currency.Inr), 0),
            new PayrollLineInput(zeroNetEmployee, Money.Of(0m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(0m, Currency.Inr), 0));

        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(currentRun);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PriorFinalizedPayrollRunSpecification>(), Arg.Any<CancellationToken>()).Returns(priorRun);
        _payrollSettings.FirstOrDefaultAsync(Arg.Any<PayrollSettingsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((PayrollSettings?)null);

        var result = await CreateHandler().Handle(new GetPayrollRunVarianceQuery(currentRun.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var lines = result.Value;

        lines.Should().Contain(l => l.EmployeeId == stableEmployee.Value && l.Flags.Count == 0);
        lines.Should().Contain(l => l.EmployeeId == largeVarianceEmployee.Value && l.Flags.Contains(nameof(PayrollVarianceFlag.LargeVariance)));
        lines.Should().Contain(l => l.EmployeeId == newJoiner.Value && l.Flags.Contains(nameof(PayrollVarianceFlag.NewJoiner)));
        lines.Should().Contain(l => l.EmployeeId == exitedEmployee.Value && l.Flags.Contains(nameof(PayrollVarianceFlag.Exit)) && l.CurrentNet == null);
        lines.Should().Contain(l => l.EmployeeId == zeroNetEmployee.Value && l.Flags.Contains(nameof(PayrollVarianceFlag.ZeroNet)));
    }

    [Fact]
    public async Task Handle_Should_Report_No_Flags_When_There_Is_No_Prior_Run()
    {
        var now = DateTimeOffset.UtcNow;
        var employeeId = EmployeeId.New();
        var currentRun = RunWithLines(_tenantId, now,
            new PayrollLineInput(employeeId, Money.Of(50000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(50000m, Currency.Inr), 0));

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(currentRun);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PriorFinalizedPayrollRunSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);
        _payrollSettings.FirstOrDefaultAsync(Arg.Any<PayrollSettingsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((PayrollSettings?)null);

        var result = await CreateHandler().Handle(new GetPayrollRunVarianceQuery(currentRun.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(l => l.EmployeeId == employeeId.Value && l.Flags.Count == 0 && l.PriorNet == null);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new GetPayrollRunVarianceQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
