using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

public sealed record OpenPayrollRunCommand(int Month, int Year, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
