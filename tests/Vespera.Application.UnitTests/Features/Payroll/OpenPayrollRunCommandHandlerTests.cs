using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class OpenPayrollRunCommandHandlerTests
{
    private readonly IWriteRepository<PayrollRun> _payrollRuns = Substitute.For<IWriteRepository<PayrollRun>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private OpenPayrollRunCommandHandler CreateHandler() => new(_payrollRuns, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Open_A_New_Run_And_Return_Its_Id()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new OpenPayrollRunCommand(5, 2026, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _payrollRuns.Received(1).AddAsync(Arg.Is<PayrollRun>(r => r.Month == 5 && r.Year == 2026), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_For_An_Invalid_Month()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);

        var result = await CreateHandler().Handle(new OpenPayrollRunCommand(13, 2026, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.invalid_month");
        await _payrollRuns.DidNotReceive().AddAsync(Arg.Any<PayrollRun>(), Arg.Any<CancellationToken>());
    }
}
