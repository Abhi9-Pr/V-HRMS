using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance;

public sealed record ResolveQuarantinedPunchCommand(Guid QuarantinedBiometricPunchId, Guid EmployeeId) : IRequest<Result>;
