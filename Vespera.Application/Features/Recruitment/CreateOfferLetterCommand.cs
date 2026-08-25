using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Recruitment;

public sealed record CreateOfferLetterCommand(
    Guid CandidateId, Guid ProposedDesignationId, decimal ProposedCtc, Currency Currency, DateOnly JoiningDate, string? IdempotencyKey)
    : IRequest<Result<Guid>>, IIdempotentRequest;
