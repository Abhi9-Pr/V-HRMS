using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetPayrollRunsQueryHandlerTests
{
    private readonly IReadRepository<PayrollRun> _payrollRuns = Substitute.For<IReadRepository<PayrollRun>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();

    private GetPayrollRunsQueryHandler CreateHandler() =>
        new(_payrollRuns, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    [Fact]
    public async Task Handle_Should_Return_A_Paged_List_Of_Run_Summaries_And_Record_Pii_Access()
    {
        var tenantId = TenantId.New();
        var now = DateTimeOffset.UtcNow;
        var run = PayrollRun.Open(tenantId, 5, 2026, now, "seed").Value;

        _tenantContext.TenantId.Returns(tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payrollRuns.ListAsync(Arg.Any<PayrollRunsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([run]);
        _payrollRuns.CountAsync(Arg.Any<PayrollRunsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new GetPayrollRunsQuery(new PagedRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(item => item.Id == run.Id.Value);
        result.Value.TotalCount.Should().Be(1);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            tenantId, "PayrollRun", Guid.Empty, "List", "system", now, Arg.Any<CancellationToken>());
    }
}
