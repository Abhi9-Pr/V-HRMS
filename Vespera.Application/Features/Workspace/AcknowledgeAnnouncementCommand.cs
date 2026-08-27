using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record AcknowledgeAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
