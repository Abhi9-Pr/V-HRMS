using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;

namespace Vespera.Application.Behaviors;

/// <summary>
/// The single place SaveChangesAsync is called. Handlers stage changes via IWriteRepository
/// and must never call IUnitOfWork themselves. Persists only when the handler succeeded, since
/// a failed Result means nothing was validly staged.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (response.IsSuccess)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
