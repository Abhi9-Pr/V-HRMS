using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>Self-service counterpart to <c>GetPayslipDownloadUrlQuery</c> (which stays
/// Finance.Admin-gated for looking up *any* employee's payslip): resolves the caller's own most
/// recent published payslip and its signed download URL in one call. Closes the gap noted on
/// <c>PayslipsController</c> ("Employee self-download... is not yet wired") for the dashboard's
/// payslip quick-link widget.</summary>
public sealed record GetMyLatestPayslipQuery : IRequest<Result<MyLatestPayslipDto?>>;

public sealed record MyLatestPayslipDto(Guid PayslipId, DateTimeOffset GeneratedAt, decimal NetPay, string Currency, Uri DownloadUrl);
