using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record ConfirmAssetsRecoveredCommand(Guid EmployeeId) : IRequest<Result>;
