using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed record UpdateRotationPatternCommand(Guid Id, IReadOnlyList<RotationPatternDayRequest> Days) : IRequest<Result>;
