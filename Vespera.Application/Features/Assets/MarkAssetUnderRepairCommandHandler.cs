using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class MarkAssetUnderRepairCommandHandler : IRequestHandler<MarkAssetUnderRepairCommand, Result>
{
    private readonly IReadRepository<Asset> _assets;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkAssetUnderRepairCommandHandler(IReadRepository<Asset> assets, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _assets = assets;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(MarkAssetUnderRepairCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assets.FirstOrDefaultAsync(new AssetByIdSpecification(new AssetId(request.AssetId)), cancellationToken);
        if (asset is null)
        {
            return Result.Failure(Error.NotFound("asset.not_found", "Asset not found."));
        }

        return asset.MarkUnderRepair(_dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system");
    }
}
