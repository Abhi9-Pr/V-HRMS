using Mapster;
using Vespera.Domain.Assets;

namespace Vespera.Application.Features.Assets;

public sealed class AssetMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Asset, AssetDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.PurchaseCost, src => src.PurchaseCost.Amount)
            .Map(dest => dest.PurchaseCostCurrency, src => src.PurchaseCost.Currency)
            .Map(dest => dest.Depreciation, src => src.Depreciation == null ? null : new DepreciationScheduleDto(
                src.Depreciation.Method, src.Depreciation.UsefulLifeMonths, src.Depreciation.SalvageValue.Amount, src.Depreciation.SalvageValue.Currency));

        config.NewConfig<AssetAssignment, AssetAssignmentSummaryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.EmployeeId, src => src.EmployeeId.Value);
    }
}
