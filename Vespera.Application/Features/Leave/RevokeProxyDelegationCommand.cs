using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record RevokeProxyDelegationCommand(Guid DelegationId) : IRequest<Result>;
