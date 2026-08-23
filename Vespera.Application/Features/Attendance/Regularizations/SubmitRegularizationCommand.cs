using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

/// <summary>Submitted by the employee themselves, about their own <c>AttendanceDay</c> — the
/// employee identity comes from the day being regularized (<paramref name="AttendanceDayId"/>),
/// and the handler checks the caller's linked employee matches it, the same self-only rule
/// <c>SubordinateOrSelfRequirement</c> applies elsewhere for a caller acting on their own record.</summary>
public sealed record SubmitRegularizationCommand(
    Guid AttendanceDayId, string Reason, string? EvidenceFileName, byte[]? EvidenceContent) : IRequest<Result<Guid>>;
