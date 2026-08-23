using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record GetPayslipDownloadUrlQuery(Guid PayslipId) : IRequest<Result<Uri>>;
