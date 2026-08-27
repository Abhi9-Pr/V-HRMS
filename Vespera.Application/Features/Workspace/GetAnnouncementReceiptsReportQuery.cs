using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record GetAnnouncementReceiptsReportQuery(Guid AnnouncementId) : IRequest<Result<AnnouncementReceiptsReportDto>>;

public sealed record AnnouncementReceiptRowDto(Guid EmployeeId, string EmployeeName, bool Acknowledged, DateTimeOffset? AcknowledgedAt);

public sealed record AnnouncementReceiptsReportDto(
    Guid AnnouncementId, string Title, int TargetEmployeeCount, int AcknowledgedCount, IReadOnlyList<AnnouncementReceiptRowDto> Rows);
