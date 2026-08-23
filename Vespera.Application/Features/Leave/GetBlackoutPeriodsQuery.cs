using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record GetBlackoutPeriodsQuery : IRequest<Result<IReadOnlyList<BlackoutPeriodDto>>>;

public sealed record BlackoutPeriodDto(Guid Id, DateOnly From, DateOnly To, string Reason, Guid? LeaveTypeId);
