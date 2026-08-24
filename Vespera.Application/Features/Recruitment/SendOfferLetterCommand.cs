using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record SendOfferLetterCommand(Guid OfferLetterId, string? IdempotencyKey) : IRequest<Result>, IIdempotentRequest;
