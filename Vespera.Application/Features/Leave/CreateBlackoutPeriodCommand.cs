using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record CreateBlackoutPeriodCommand(DateOnly From, DateOnly To, string Reason, Guid? LeaveTypeId) : IRequest<Result<Guid>>;
