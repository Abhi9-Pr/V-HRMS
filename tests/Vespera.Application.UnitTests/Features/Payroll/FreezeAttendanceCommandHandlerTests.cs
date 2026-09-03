using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class FreezeAttendanceCommandHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly IReadRepository<PayrollSettings> _payrollSettings = Substitute.For<IReadRepository<PayrollSettings>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private FreezeAttendanceCommandHandler CreateHandler() =>
        new(_payrollRuns, _payrollSettings, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Freeze_The_Run_Using_The_Default_FreezeDay_When_No_Settings_Exist()
    {
        var now = new DateTimeOffset(2026, 5, 26, 0, 0, 0, TimeSpan.Zero);
        var run = PayrollRun.Open(_tenantId, 5, 2026, now, "system").Value;

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _payrollSettings.FirstOrDefaultAsync(Arg.Any<PayrollSettingsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((PayrollSettings?)null);

        var result = await CreateHandler().Handle(new FreezeAttendanceCommand(run.Id.Value, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        run.Status.Should().Be(PayrollRunStatus.AttendanceFrozen);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Freezing_Early_Without_An_Override_Reason()
    {
        var now = new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);
        var run = PayrollRun.Open(_tenantId, 5, 2026, now, "system").Value;

        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);
        _payrollSettings.FirstOrDefaultAsync(Arg.Any<PayrollSettingsByTenantSpecification>(), Arg.Any<CancellationToken>())
            .Returns((PayrollSettings?)null);

        var result = await CreateHandler().Handle(new FreezeAttendanceCommand(run.Id.Value, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.freeze_override_reason_required");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new FreezeAttendanceCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
    }
}
