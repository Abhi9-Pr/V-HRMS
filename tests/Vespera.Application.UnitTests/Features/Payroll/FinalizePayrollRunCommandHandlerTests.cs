using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class FinalizePayrollRunCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private FinalizePayrollRunCommandHandler CreateHandler() => new(_payrollRuns, _dateTimeProvider);

    private static PayrollRun ApprovedRun(TenantId tenantId, DateTimeOffset now)
    {
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), freezeDay: 25, now, "seed");
        run.RecomputeLines(
            [new PayrollLineInput(EmployeeId.New(), Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        run.SubmitForReview();
        run.Approve("seed", now);
        return run;
    }

    [Fact]
    public async Task Handle_Should_Finalize_An_Approved_Run()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = ApprovedRun(tenantId, now);

        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new FinalizePayrollRunCommand(run.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Finalized);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Is_Not_Approved()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;

        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new FinalizePayrollRunCommand(run.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_approved");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new FinalizePayrollRunCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
