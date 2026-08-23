using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed record RejectRegularizationCommand(Guid RequestId, string RejectionReason) : IRequest<Result>;
