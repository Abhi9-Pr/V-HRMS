using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.RotationPatterns;

public sealed record DeleteRotationPatternCommand(Guid Id) : IRequest<Result>;
