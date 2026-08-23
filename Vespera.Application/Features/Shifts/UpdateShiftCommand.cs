using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed record UpdateShiftCommand(Guid Id, string Name, TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes) : IRequest<Result>;
