using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record CreateReportingRelationshipCommand(
    Guid EmployeeId,
    Guid ManagerId,
    DateOnly ValidFrom,
    DateOnly? ValidTo) : IRequest<Result<Guid>>;
