using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Helpdesk;

public sealed record GetSlaComplianceReportQuery : IRequest<Result<SlaComplianceReportDto>>;

public sealed record SlaComplianceReportDto(
    int TotalResolvedOrClosed, int BreachedCount, int OnTimeCount, double CompliancePercentage,
    IReadOnlyList<CategoryComplianceRowDto> ByCategory);

public sealed record CategoryComplianceRowDto(Guid CategoryId, string CategoryName, int Total, int Breached, double CompliancePercentage);
