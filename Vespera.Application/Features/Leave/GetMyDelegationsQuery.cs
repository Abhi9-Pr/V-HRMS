using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Leave;

public sealed record GetMyDelegationsQuery : IRequest<Result<IReadOnlyList<ProxyDelegationDto>>>;

public sealed record ProxyDelegationDto(Guid Id, Guid DelegateEmployeeId, DateOnly From, DateOnly To, string Scope, bool IsRevoked);
