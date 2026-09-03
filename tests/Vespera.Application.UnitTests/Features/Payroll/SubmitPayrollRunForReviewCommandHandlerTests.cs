using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class SubmitPayrollRunForReviewCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();

    private SubmitPayrollRunForReviewCommandHandler CreateHandler() => new(_payrollRuns);

    [Fact]
    public async Task Handle_Should_Submit_A_Computed_DryRun_For_Review()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), freezeDay: 25, now, "seed");
        run.RecomputeLines(
            [new PayrollLineInput(EmployeeId.New(), Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);

        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new SubmitPayrollRunForReviewCommand(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Review);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Is_Not_A_Computed_DryRun()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;

        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new SubmitPayrollRunForReviewCommand(run.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_dry_run");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new SubmitPayrollRunForReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
