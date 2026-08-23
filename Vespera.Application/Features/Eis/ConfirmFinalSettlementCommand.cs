using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Eis;

public sealed record ConfirmFinalSettlementCommand(Guid EmployeeId) : IRequest<Result>;
