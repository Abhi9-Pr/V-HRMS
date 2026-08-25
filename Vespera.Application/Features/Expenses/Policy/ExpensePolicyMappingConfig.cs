using Mapster;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses.Policy;

public sealed class ExpensePolicyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ExpensePolicy, ExpensePolicyDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.MaxAmountPerClaim, src => src.MaxAmountPerClaim.Amount)
            .Map(dest => dest.ReceiptRequiredAboveAmount, src => src.ReceiptRequiredAboveAmount.Amount)
            .Map(dest => dest.Currency, src => src.MaxAmountPerClaim.Currency)
            .Map(dest => dest.ApplicableDesignationId, src => src.ApplicableDesignationId != null ? src.ApplicableDesignationId.Value.Value : (Guid?)null);
    }
}
