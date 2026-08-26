using MediatR;
using Vespera.Domain.Common;
using Vespera.Domain.Recruitment;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetOfferLetterByIdQuery(Guid Id) : IRequest<Result<OfferLetterDto>>;

public sealed record OfferLetterDto(
    Guid Id, Guid CandidateId, Guid ProposedDesignationId, decimal ProposedCtc, string Currency, DateOnly JoiningDate, OfferLetterStatus Status);
