using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed class ReturnAssetCommandHandler : IRequestHandler<ReturnAssetCommand, Result>
{
    private readonly IReadRepository<AssetAssignment> _assignments;
    private readonly IReadRepository<Asset> _assets;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReturnAssetCommandHandler(
        IReadRepository<AssetAssignment> assignments, IReadRepository<Asset> assets, ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _assignments = assignments;
        _assets = assets;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReturnAssetCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignments.FirstOrDefaultAsync(
            new AssetAssignmentByIdSpecification(new AssetAssignmentId(request.AssignmentId)), cancellationToken);

        if (assignment is null)
        {
            return Result.Failure(Error.NotFound("asset_assignment.not_found", "Asset assignment not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var returnResult = assignment.Return(request.Condition, now);
        if (returnResult.IsFailure)
        {
            return returnResult;
        }

        var asset = await _assets.FirstOrDefaultAsync(new AssetByIdSpecification(assignment.AssetId), cancellationToken);
        if (asset is null)
        {
            return Result.Failure(Error.NotFound("asset.not_found", "Asset not found."));
        }

        return asset.ReturnToStock(now, _currentUser.UserId?.ToString() ?? "system");
    }
}
