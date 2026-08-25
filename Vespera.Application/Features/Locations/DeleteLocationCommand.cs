using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Locations;

public sealed record DeleteLocationCommand(Guid Id) : IRequest<Result>;
