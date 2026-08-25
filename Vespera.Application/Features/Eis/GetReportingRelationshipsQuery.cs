using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record GetReportingRelationshipsQuery(Guid EmployeeId) : IRequest<Result<IReadOnlyList<ReportingRelationshipDto>>>;

public sealed record ReportingRelationshipDto(Guid Id, Guid EmployeeId, Guid ManagerId, DateOnly ValidFrom, DateOnly? ValidTo);
