using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record ClearPunchFlagCommand(Guid EmployeeId, DateOnly Date, Guid PunchId) : IRequest<Result>;
