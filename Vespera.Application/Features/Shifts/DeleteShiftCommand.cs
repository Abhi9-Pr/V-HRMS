using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed record DeleteShiftCommand(Guid Id) : IRequest<Result>;
