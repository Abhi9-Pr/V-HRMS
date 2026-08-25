using MediatR;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class CompleteAssetRecoveryCommandHandler : IRequestHandler<CompleteAssetRecoveryCommand, Result>
{
    private readonly IReadRepository<AssetRecovery> _recoveries;

    public CompleteAssetRecoveryCommandHandler(IReadRepository<AssetRecovery> recoveries)
    {
        _recoveries = recoveries;
    }

    public async Task<Result> Handle(CompleteAssetRecoveryCommand request, CancellationToken cancellationToken)
    {
        var recovery = await _recoveries.FirstOrDefaultAsync(
            new AssetRecoveryByIdSpecification(new AssetRecoveryId(request.RecoveryId)), cancellationToken);

        if (recovery is null)
        {
            return Result.Failure(Error.NotFound("asset_recovery.not_found", "Asset recovery not found."));
        }

        return recovery.Complete();
    }
}
