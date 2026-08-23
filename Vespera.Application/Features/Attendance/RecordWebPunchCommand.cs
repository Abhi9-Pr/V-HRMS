using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record RecordWebPunchCommand(Guid EmployeeId, string PunchType, double? Latitude, double? Longitude)
    : IRequest<Result>;
