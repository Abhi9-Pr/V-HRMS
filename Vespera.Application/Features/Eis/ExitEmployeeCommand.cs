using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Eis;

public sealed record ExitEmployeeCommand(
    Guid EmployeeId, DateOnly ExitDate, EmployeeExitReason Reason, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
