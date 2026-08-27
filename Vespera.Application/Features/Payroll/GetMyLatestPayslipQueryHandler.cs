using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

public sealed class GetMyLatestPayslipQueryHandler : IRequestHandler<GetMyLatestPayslipQuery, Result<MyLatestPayslipDto?>>
{
    private static readonly TimeSpan LinkExpiry = TimeSpan.FromMinutes(15);

    private readonly IReadRepository<Payslip> _payslips;
    private readonly IFileStorage _fileStorage;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetMyLatestPayslipQueryHandler(
        IReadRepository<Payslip> payslips, IFileStorage fileStorage, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _payslips = payslips;
        _fileStorage = fileStorage;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<MyLatestPayslipDto?>> Handle(GetMyLatestPayslipQuery request, CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<MyLatestPayslipDto?>(null);
        }

        var payslips = await _payslips.ListAsync(
            new PayslipsByEmployeeSpecification(_tenantContext.TenantId, employeeId.Value), cancellationToken);
        var latest = payslips.FirstOrDefault(p => p.StorageKey is not null);
        if (latest is null)
        {
            return Result.Success<MyLatestPayslipDto?>(null);
        }

        await _piiAccessAuditor.RecordAccessAsync(
            _tenantContext.TenantId, "Payslip", latest.Id.Value, "Document", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        var url = await _fileStorage.GetDownloadUrlAsync(latest.StorageKey!, LinkExpiry, cancellationToken);

        return Result.Success<MyLatestPayslipDto?>(new MyLatestPayslipDto(
            latest.Id.Value, latest.GeneratedAt, latest.NetPay.Amount, latest.NetPay.Currency.ToString(), url));
    }
}
