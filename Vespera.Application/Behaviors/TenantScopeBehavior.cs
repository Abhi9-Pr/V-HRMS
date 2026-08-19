using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Domain.Common;

namespace Vespera.Application.Behaviors;

public sealed class TenantScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly ITenantContext _tenantContext;

    public TenantScopeBehavior(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_tenantContext.HasTenant)
        {
            return ResultResponseFactory.Create<TResponse>(
                Error.Unauthorized("tenant.missing", "This request requires an authenticated tenant context."));
        }

        return await next();
    }
}
