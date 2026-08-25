using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

/// <summary>Writing an asset off also retires it — a written-off asset is no longer usable
/// inventory, so both aggregates are mutated in the same handler.</summary>
public sealed class WriteOffAssetCommandHandler : IRequestHandler<WriteOffAssetCommand, Result>
{
    private readonly IReadRepository<AssetRecovery> _recoveries;
    private readonly IReadRepository<Asset> _assets;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WriteOffAssetCommandHandler(
        IReadRepository<AssetRecovery> recoveries, IReadRepository<Asset> assets, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _recoveries = recoveries;
        _assets = assets;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(WriteOffAssetCommand request, CancellationToken cancellationToken)
    {
        var recovery = await _recoveries.FirstOrDefaultAsync(
            new AssetRecoveryByIdSpecification(new AssetRecoveryId(request.RecoveryId)), cancellationToken);

        if (recovery is null)
        {
            return Result.Failure(Error.NotFound("asset_recovery.not_found", "Asset recovery not found."));
        }

        var writeOffResult = recovery.WriteOff(Money.Of(request.Amount, request.Currency), request.Reason);
        if (writeOffResult.IsFailure)
        {
            return writeOffResult;
        }

        var asset = await _assets.FirstOrDefaultAsync(new AssetByIdSpecification(recovery.AssetId), cancellationToken);
        if (asset is null)
        {
            return Result.Failure(Error.NotFound("asset.not_found", "Asset not found."));
        }

        return asset.Retire(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
