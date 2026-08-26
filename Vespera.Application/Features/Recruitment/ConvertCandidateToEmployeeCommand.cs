using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record ConvertCandidateToEmployeeCommand(
    Guid CandidateId, Guid OfferLetterId, string EmployeeCode, DateOnly DateOfBirth, Guid LocationId, string? IdempotencyKey)
    : IRequest<Result<Guid>>, IIdempotentRequest;
