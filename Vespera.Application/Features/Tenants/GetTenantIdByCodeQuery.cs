using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Tenants;

public sealed record GetTenantIdByCodeQuery(string Code) : IRequest<Result<TenantLookupDto>>, ITenantlessRequest;

/// <summary>Just enough to resolve the pre-auth <c>X-Tenant-Id</c> header from a human-readable
/// code — never anything sensitive, since this endpoint is deliberately anonymous.</summary>
public sealed record TenantLookupDto(Guid TenantId, string Name);
