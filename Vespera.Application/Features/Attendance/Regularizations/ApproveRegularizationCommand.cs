using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Attendance.Regularizations;

public sealed record ApproveRegularizationCommand(Guid RequestId) : IRequest<Result>;
