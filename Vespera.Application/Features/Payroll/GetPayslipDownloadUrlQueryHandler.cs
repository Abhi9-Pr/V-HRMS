using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Application.Features.Payroll;

/// <summary>Authorize-once-at-issue-time, same pattern as <c>FilesController</c>'s employee-document
/// downloads: this query checks tenant ownership and hands back a short-lived signed URL; the
/// download request itself re-verifies only the signature and expiry, never re-runs a permission
/// check. Currently reachable only through <c>PayslipsController</c>'s Finance.Admin wall — an
/// employee downloading their own payslip is not yet wired (see the plan's remaining-work notes).</summary>
public sealed class GetPayslipDownloadUrlQueryHandler : IRequestHandler<GetPayslipDownloadUrlQuery, Result<Uri>>
{
    private static readonly TimeSpan LinkExpiry = TimeSpan.FromMinutes(15);

    private readonly IReadRepository<Payslip> _payslips;
    private readonly IFileStorage _fileStorage;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPiiAccessAuditor _piiAccessAuditor;

    public GetPayslipDownloadUrlQueryHandler(
        IReadRepository<Payslip> payslips, IFileStorage fileStorage, ITenantContext tenantContext, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider, IPiiAccessAuditor piiAccessAuditor)
    {
        _payslips = payslips;
        _fileStorage = fileStorage;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _piiAccessAuditor = piiAccessAuditor;
    }

    public async Task<Result<Uri>> Handle(GetPayslipDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var payslip = await _payslips.FirstOrDefaultAsync(new PayslipByIdSpecification(new PayslipId(request.PayslipId)), cancellationToken);
        if (payslip is null || payslip.TenantId != _tenantContext.TenantId)
        {
            return Result.Failure<Uri>(Error.NotFound("payslip.not_found", "Payslip not found."));
        }

        if (payslip.StorageKey is null)
        {
            return Result.Failure<Uri>(Error.Conflict("payslip.document_not_ready", "This payslip's document has not been generated yet."));
        }

        await _piiAccessAuditor.RecordAccessAsync(
            _tenantContext.TenantId, "Payslip", payslip.Id.Value, "Document", _currentUser.UserId?.ToString() ?? "system",
            _dateTimeProvider.UtcNow, cancellationToken);

        var url = await _fileStorage.GetDownloadUrlAsync(payslip.StorageKey, LinkExpiry, cancellationToken);
        return Result.Success(url);
    }
}
