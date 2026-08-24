using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Recruitment;

public sealed record GenerateOfferLetterPdfQuery(Guid OfferLetterId) : IRequest<Result<byte[]>>;
