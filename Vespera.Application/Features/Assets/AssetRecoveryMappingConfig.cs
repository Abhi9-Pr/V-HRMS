using Mapster;
using Vespera.Domain.Assets;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Assets;

public sealed class AssetRecoveryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AssetRecovery, AssetRecoveryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.AssetAssignmentId, src => src.AssetAssignmentId.Value)
            .Map(dest => dest.AssetId, src => src.AssetId.Value)
            .Map(dest => dest.EmployeeId, src => src.EmployeeId.Value)
            .Map(dest => dest.WriteOffAmount, src => src.WriteOffAmount == null ? (decimal?)null : src.WriteOffAmount.Amount)
            .Map(dest => dest.WriteOffAmountCurrency, src => src.WriteOffAmount == null ? (Currency?)null : src.WriteOffAmount.Currency);
    }
}
