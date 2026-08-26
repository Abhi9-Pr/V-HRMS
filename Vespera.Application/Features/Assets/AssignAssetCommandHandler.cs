using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.Features.Assets;

public sealed class AssignAssetCommandHandler : IRequestHandler<AssignAssetCommand, Result<Guid>>
{
    private readonly IReadRepository<Asset> _assets;
    private readonly IWriteRepository<AssetAssignment> _assignments;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignAssetCommandHandler(
        IReadRepository<Asset> assets, IWriteRepository<AssetAssignment> assignments, ITenantContext tenantContext,
        ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _assets = assets;
        _assignments = assignments;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assets.FirstOrDefaultAsync(new AssetByIdSpecification(new AssetId(request.AssetId)), cancellationToken);
        if (asset is null)
        {
            return Result.Failure<Guid>(Error.NotFound("asset.not_found", "Asset not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var markResult = asset.MarkAssigned(now, _currentUser.UserId?.ToString() ?? "system");
        if (markResult.IsFailure)
        {
            return Result.Failure<Guid>(markResult.Error);
        }

        var assignment = AssetAssignment.Assign(_tenantContext.TenantId, asset.Id, new EmployeeId(request.EmployeeId), now);
        await _assignments.AddAsync(assignment, cancellationToken);

        return Result.Success(assignment.Id.Value);
    }
}
