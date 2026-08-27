using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Workspace;

public sealed record SetAnnouncementPinnedCommand(Guid AnnouncementId, bool Pinned) : IRequest<Result>;
