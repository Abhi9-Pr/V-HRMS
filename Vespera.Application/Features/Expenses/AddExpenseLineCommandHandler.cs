using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

/// <summary>Adds a line to a Draft claim, converting into the claim's settlement currency using the
/// transaction-date rate. A same-day cached <see cref="CurrencyRate"/> is reused; a miss calls
/// <see cref="ICurrencyRateProvider"/> once and caches the result for next time.</summary>
public sealed class AddExpenseLineCommandHandler : IRequestHandler<AddExpenseLineCommand, Result>
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IReadRepository<CurrencyRate> _readCurrencyRates;
    private readonly IWriteRepository<CurrencyRate> _writeCurrencyRates;
    private readonly ICurrencyRateProvider _currencyRateProvider;
    private readonly ITenantContext _tenantContext;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public AddExpenseLineCommandHandler(
        IReadRepository<ExpenseClaim> expenseClaims,
        IReadRepository<CurrencyRate> readCurrencyRates,
        IWriteRepository<CurrencyRate> writeCurrencyRates,
        ICurrencyRateProvider currencyRateProvider,
        ITenantContext tenantContext,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _expenseClaims = expenseClaims;
        _readCurrencyRates = readCurrencyRates;
        _writeCurrencyRates = writeCurrencyRates;
        _currencyRateProvider = currencyRateProvider;
        _tenantContext = tenantContext;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(AddExpenseLineCommand request, CancellationToken cancellationToken)
    {
        var claim = await _expenseClaims.FirstOrDefaultAsync(
            new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.ClaimId)), cancellationToken);

        if (claim is null)
        {
            return Result.Failure(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null || claim.EmployeeId != employeeId.Value)
        {
            return Result.Failure(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        Money? convertedAmount = null;
        decimal? exchangeRate = null;

        if (request.Currency != claim.SettlementCurrency)
        {
            exchangeRate = await ResolveRateAsync(request.Currency, claim.SettlementCurrency, request.ExpenseDate, cancellationToken);
            convertedAmount = Money.Of(Math.Round(request.Amount * exchangeRate.Value, 2), claim.SettlementCurrency);
        }

        var result = claim.AddLine(
            request.Category,
            Money.Of(request.Amount, request.Currency),
            request.ExpenseDate,
            request.ReceiptReference,
            request.Vendor,
            request.TaxAmount is { } taxAmount ? Money.Of(taxAmount, request.Currency) : null,
            convertedAmount,
            exchangeRate);

        return result;
    }

    private async Task<decimal> ResolveRateAsync(Currency from, Currency to, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        var cachedRate = await _readCurrencyRates.FirstOrDefaultAsync(
            new CurrencyRateBySpecification(_tenantContext.TenantId, from, to, effectiveDate), cancellationToken);

        if (cachedRate is not null)
        {
            return cachedRate.Rate;
        }

        var rate = await _currencyRateProvider.GetRateAsync(from.ToIsoCode(), to.ToIsoCode(), effectiveDate, cancellationToken);

        var cacheResult = CurrencyRate.Create(_tenantContext.TenantId, from, to, rate, effectiveDate);
        if (cacheResult.IsSuccess)
        {
            await _writeCurrencyRates.AddAsync(cacheResult.Value, cancellationToken);
        }

        return rate;
    }
}
