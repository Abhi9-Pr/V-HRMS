using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record CancelCorporateEventCommand(Guid CorporateEventId) : IRequest<Result>;
