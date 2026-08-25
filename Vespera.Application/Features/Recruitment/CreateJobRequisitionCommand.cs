using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record CreateJobRequisitionCommand(
    string Title, Guid DepartmentId, int OpeningsCount, string? IdempotencyKey) : IRequest<Result<Guid>>, IIdempotentRequest;
