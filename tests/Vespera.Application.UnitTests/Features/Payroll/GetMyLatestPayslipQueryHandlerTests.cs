using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetMyLatestPayslipQueryHandlerTests
{
    private readonly IReadRepository<Payslip> _payslips = Substitute.For<IReadRepository<Payslip>>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetMyLatestPayslipQueryHandler CreateHandler() => new(
        _payslips, _fileStorage, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor,
        new CurrentEmployeeResolver(_users, _currentUser));

    private void SetUpAuthenticatedUserWithEmployee(EmployeeId employeeId)
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, DateTimeOffset.UtcNow, "seed");
        _users.FirstOrDefaultAsync(Arg.Any<Vespera.Application.Features.Auth.UserByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(user);
    }

    [Fact]
    public async Task Handle_Should_Return_The_Latest_Payslip_With_A_Download_Url_And_Record_Pii_Access()
    {
        var now = DateTimeOffset.UtcNow;
        var employeeId = EmployeeId.New();
        SetUpAuthenticatedUserWithEmployee(employeeId);
        _dateTimeProvider.UtcNow.Returns(now);

        var payslip = Payslip.Generate(_tenantId, PayrollRunId.New(), employeeId, Money.Of(40000m, Currency.Inr), [], now);
        payslip.AttachDocument("payslips/key.pdf", "hash", now);
        _payslips.ListAsync(Arg.Any<PayslipsByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns([payslip]);

        var downloadUrl = new Uri("https://storage.vespera.test/payslips/key.pdf?sig=abc");
        _fileStorage.GetDownloadUrlAsync("payslips/key.pdf", TimeSpan.FromMinutes(15), Arg.Any<CancellationToken>()).Returns(downloadUrl);

        var result = await CreateHandler().Handle(new GetMyLatestPayslipQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.PayslipId.Should().Be(payslip.Id.Value);
        result.Value.DownloadUrl.Should().Be(downloadUrl);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "Payslip", payslip.Id.Value, "Document", Arg.Any<string>(), now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_No_Payslip_Has_A_Document_Attached()
    {
        var employeeId = EmployeeId.New();
        SetUpAuthenticatedUserWithEmployee(employeeId);

        var payslip = Payslip.Generate(_tenantId, PayrollRunId.New(), employeeId, Money.Of(40000m, Currency.Inr), [], DateTimeOffset.UtcNow);
        _payslips.ListAsync(Arg.Any<PayslipsByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns([payslip]);

        var result = await CreateHandler().Handle(new GetMyLatestPayslipQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyLatestPayslipQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }
}
