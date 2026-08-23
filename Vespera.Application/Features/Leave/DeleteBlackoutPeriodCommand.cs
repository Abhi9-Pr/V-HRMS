using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record DeleteBlackoutPeriodCommand(Guid Id) : IRequest<Result>;
