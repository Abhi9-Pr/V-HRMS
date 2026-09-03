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

public class ApprovePayrollRunCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private ApprovePayrollRunCommandHandler CreateHandler() => new(_payrollRuns, _currentUser, _dateTimeProvider);

    private static PayrollRun RunInReview(TenantId tenantId, DateTimeOffset now)
    {
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;
        run.FreezeAttendance(new DateOnly(2026, 5, 26), freezeDay: 25, now, "seed");
        run.RecomputeLines(
            [new PayrollLineInput(EmployeeId.New(), Money.Of(40000m, Currency.Inr), Money.Of(0m, Currency.Inr), Money.Of(40000m, Currency.Inr), 0)],
            "seed", now);
        run.SubmitForReview();
        return run;
    }

    [Fact]
    public async Task Handle_Should_Approve_A_Run_Under_Review()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = RunInReview(tenantId, now);

        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new ApprovePayrollRunCommand(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.Approved);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Is_Not_In_Review()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;

        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new ApprovePayrollRunCommand(run.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_in_review");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new ApprovePayrollRunCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
