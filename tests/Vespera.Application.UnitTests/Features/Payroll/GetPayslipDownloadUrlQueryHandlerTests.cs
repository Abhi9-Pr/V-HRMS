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

public class GetPayslipDownloadUrlQueryHandlerTests
{
    private readonly IReadRepository<Payslip> _payslips = Substitute.For<IReadRepository<Payslip>>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetPayslipDownloadUrlQueryHandler CreateHandler() =>
        new(_payslips, _fileStorage, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    [Fact]
    public async Task Handle_Should_Return_A_Signed_Download_Url_And_Record_Pii_Access()
    {
        var now = DateTimeOffset.UtcNow;
        var payslip = Payslip.Generate(_tenantId, PayrollRunId.New(), EmployeeId.New(), Money.Of(40000m, Currency.Inr), [], now);
        payslip.AttachDocument("payslips/key.pdf", "hash", now);

        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payslip);
        var downloadUrl = new Uri("https://storage.vespera.test/payslips/key.pdf?sig=abc");
        _fileStorage.GetDownloadUrlAsync("payslips/key.pdf", TimeSpan.FromMinutes(15), Arg.Any<CancellationToken>()).Returns(downloadUrl);

        var result = await CreateHandler().Handle(new GetPayslipDownloadUrlQuery(payslip.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(downloadUrl);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "Payslip", payslip.Id.Value, "Document", "system", now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Payslip_Document_Has_Not_Been_Generated_Yet()
    {
        var payslip = Payslip.Generate(_tenantId, PayrollRunId.New(), EmployeeId.New(), Money.Of(40000m, Currency.Inr), [], DateTimeOffset.UtcNow);

        _tenantContext.TenantId.Returns(_tenantId);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payslip);

        var result = await CreateHandler().Handle(new GetPayslipDownloadUrlQuery(payslip.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payslip.document_not_ready");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Payslip_Belongs_To_A_Different_Tenant()
    {
        var otherTenantId = TenantId.New();
        var payslip = Payslip.Generate(otherTenantId, PayrollRunId.New(), EmployeeId.New(), Money.Of(40000m, Currency.Inr), [], DateTimeOffset.UtcNow);
        payslip.AttachDocument("payslips/key.pdf", "hash", DateTimeOffset.UtcNow);

        _tenantContext.TenantId.Returns(_tenantId);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(payslip);

        var result = await CreateHandler().Handle(new GetPayslipDownloadUrlQuery(payslip.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payslip.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Payslip_Does_Not_Exist()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _payslips.FirstOrDefaultAsync(Arg.Any<PayslipByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((Payslip?)null);

        var result = await CreateHandler().Handle(new GetPayslipDownloadUrlQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("payslip.not_found");
    }
}
