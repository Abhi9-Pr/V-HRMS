using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record RejectLeaveRequestCommand(Guid LeaveRequestId, string Reason) : IRequest<Result>;
