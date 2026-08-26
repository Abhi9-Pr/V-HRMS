using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record EncashLeaveCommand(Guid LeaveTypeId, decimal Days) : IRequest<Result>;
