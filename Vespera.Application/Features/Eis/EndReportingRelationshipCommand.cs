using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record EndReportingRelationshipCommand(Guid EmployeeId, Guid Id, DateOnly ValidTo) : IRequest<Result>;
