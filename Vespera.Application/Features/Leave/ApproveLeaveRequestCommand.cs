using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record ApproveLeaveRequestCommand(Guid LeaveRequestId, string? Comment) : IRequest<Result>;
