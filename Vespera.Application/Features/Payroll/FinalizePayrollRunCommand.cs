using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Payroll;

/// <summary>Finalization must never silently re-run: <see cref="IIdempotentRequest"/> makes a
/// retried request with the same key replay the original result instead of re-finalizing (which
/// <c>PayrollRun.Finalize</c> would refuse outright once already Finalized, but idempotency is what
/// makes a client-side retry after a dropped response safe rather than merely rejected).</summary>
public sealed record FinalizePayrollRunCommand(Guid PayrollRunId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
