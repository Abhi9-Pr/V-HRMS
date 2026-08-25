using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed record GetRotationPatternByIdQuery(Guid Id) : IRequest<Result<RotationPatternDto>>;
