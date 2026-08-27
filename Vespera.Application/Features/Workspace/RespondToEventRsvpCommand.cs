using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Workspace;

public sealed record RespondToEventRsvpCommand(Guid CorporateEventId, RsvpResponse Response) : IRequest<Result>;
