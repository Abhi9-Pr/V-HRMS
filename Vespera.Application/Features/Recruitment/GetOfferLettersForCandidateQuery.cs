using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record GetOfferLettersForCandidateQuery(Guid CandidateId) : IRequest<Result<IReadOnlyList<OfferLetterDto>>>;
