using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record CreateLeaveTypeCommand(string Name, bool IsPaid, decimal CarryForwardLimit) : IRequest<Result<Guid>>;
