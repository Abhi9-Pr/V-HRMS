using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Result<Guid>>
{
    private readonly IWriteRepository<Asset> _assets;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateAssetCommandHandler(
        IWriteRepository<Asset> assets, ITenantContext tenantContext, ICurrentUser currentUser, IDateTimeProvider dateTimeProvider)
    {
        _assets = assets;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var result = Asset.Create(
            _tenantContext.TenantId, request.AssetTag, request.Category, Money.Of(request.PurchaseCost, request.PurchaseCostCurrency),
            request.PurchaseDate, _dateTimeProvider.UtcNow, _currentUser.UserId?.ToString() ?? "system",
            request.SerialNumber, request.MacAddress, request.WarrantyExpiryDate);

        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _assets.AddAsync(result.Value, cancellationToken);
        return Result.Success(result.Value.Id.Value);
    }
}
