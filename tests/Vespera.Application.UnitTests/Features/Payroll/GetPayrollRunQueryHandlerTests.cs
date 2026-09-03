using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetPayrollRunQueryHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();

    private GetPayrollRunQueryHandler CreateHandler() =>
        new(_payrollRuns, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    [Fact]
    public async Task Handle_Should_Return_The_Run_As_A_Dto_And_Record_Pii_Access()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(run);

        var result = await CreateHandler().Handle(new GetPayrollRunQuery(run.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(run.Id.Value);
        result.Value.Month.Should().Be(5);
        result.Value.Year.Should().Be(2026);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            tenantId, "PayrollRun", run.Id.Value, "Lines", "system", now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Run_Does_Not_Exist()
    {
        _payrollRuns.FirstOrDefaultAsync(Arg.Any<PayrollRunByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((PayrollRun?)null);

        var result = await CreateHandler().Handle(new GetPayrollRunQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payroll_run.not_found");
        await _piiAccessAuditor.DidNotReceive().RecordAccessAsync(
            Arg.Any<TenantId>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }
}
