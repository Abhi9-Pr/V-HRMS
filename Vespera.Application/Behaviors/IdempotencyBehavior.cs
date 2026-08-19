using MediatR;
using Vespera.Application.Abstractions.Messaging;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;

namespace Vespera.Application.Behaviors;

public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IIdempotencyStore _idempotencyStore;

    public IdempotencyBehavior(IIdempotencyStore idempotencyStore)
    {
        _idempotencyStore = idempotencyStore;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IIdempotentRequest { IdempotencyKey.Length: > 0 } idempotentRequest)
        {
            return await next();
        }

        var key = idempotentRequest.IdempotencyKey!;

        if (await _idempotencyStore.HasBeenProcessedAsync(key, cancellationToken))
        {
            return ResultResponseFactory.Create<TResponse>(
                Error.Conflict("idempotency.duplicate", "This request has already been processed."));
        }

        var response = await next();

        if (response.IsSuccess)
        {
            await _idempotencyStore.MarkAsProcessedAsync(key, cancellationToken);
        }

        return response;
    }
}
