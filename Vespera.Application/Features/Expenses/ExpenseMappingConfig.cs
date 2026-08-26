using Mapster;
using Vespera.Domain.Expense;

namespace Vespera.Application.Features.Expenses;

public sealed class ExpenseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ExpenseClaim, ExpenseClaimDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Total, src => src.Total(src.SettlementCurrency).Amount)
            .Map(dest => dest.Lines, src => src.Lines);

        config.NewConfig<ExpenseLine, ExpenseLineDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Amount, src => src.Amount.Amount)
            .Map(dest => dest.Currency, src => src.Amount.Currency)
            .Map(dest => dest.ConvertedAmount, src => src.ConvertedAmount != null ? src.ConvertedAmount.Amount : (decimal?)null)
            .Map(dest => dest.TaxAmount, src => src.TaxAmount != null ? src.TaxAmount.Amount : (decimal?)null);
    }
}
