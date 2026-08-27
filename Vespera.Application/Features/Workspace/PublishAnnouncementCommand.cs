using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record PublishAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
