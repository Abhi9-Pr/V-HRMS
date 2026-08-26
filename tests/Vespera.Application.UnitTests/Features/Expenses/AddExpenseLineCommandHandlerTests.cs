using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class AddExpenseLineCommandHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly IReadRepository<CurrencyRate> _readCurrencyRates = Substitute.For<IReadRepository<CurrencyRate>>();
    private readonly IWriteRepository<CurrencyRate> _writeCurrencyRates = Substitute.For<IWriteRepository<CurrencyRate>>();
    private readonly ICurrencyRateProvider _currencyRateProvider = Substitute.For<ICurrencyRateProvider>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    public AddExpenseLineCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private AddExpenseLineCommandHandler CreateHandler() => new(
        _expenseClaims, _readCurrencyRates, _writeCurrencyRates, _currencyRateProvider, _tenantContext,
        CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    private ExpenseClaim CreateOwnedDraftClaim(Currency settlementCurrency = Currency.Inr)
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, settlementCurrency);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);
        return claim;
    }

    [Fact]
    public async Task Handle_Should_Add_A_Same_Currency_Line_Without_Any_Conversion()
    {
        var claim = CreateOwnedDraftClaim();
        var handler = CreateHandler();
        var command = new AddExpenseLineCommand(
            claim.Id.Value, "Travel", 1500m, Currency.Inr, new DateOnly(2026, 1, 10), null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var line = claim.Lines.Single();
        line.ConvertedAmount.Should().BeNull();
        await _currencyRateProvider.DidNotReceiveWithAnyArgs().GetRateAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_Should_Use_A_Cached_Rate_When_One_Exists_For_The_Transaction_Date()
    {
        var claim = CreateOwnedDraftClaim();
        var effectiveDate = new DateOnly(2026, 1, 10);
        var cachedRate = CurrencyRate.Create(_tenantId, Currency.Usd, Currency.Inr, 80m, effectiveDate).Value;
        _readCurrencyRates.FirstOrDefaultAsync(Arg.Any<CurrencyRateBySpecification>(), Arg.Any<CancellationToken>()).Returns(cachedRate);

        var handler = CreateHandler();
        var command = new AddExpenseLineCommand(claim.Id.Value, "Travel", 100m, Currency.Usd, effectiveDate, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var line = claim.Lines.Single();
        line.ConvertedAmount.Should().Be(Money.Of(8000m, Currency.Inr));
        line.ExchangeRate.Should().Be(80m);
        await _currencyRateProvider.DidNotReceiveWithAnyArgs().GetRateAsync(default!, default!, default, default);
        await _writeCurrencyRates.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_Should_Fall_Back_To_The_Rate_Provider_And_Cache_The_Result_When_Nothing_Is_Cached()
    {
        var claim = CreateOwnedDraftClaim();
        var effectiveDate = new DateOnly(2026, 1, 10);
        _readCurrencyRates.FirstOrDefaultAsync(Arg.Any<CurrencyRateBySpecification>(), Arg.Any<CancellationToken>())
            .Returns((CurrencyRate?)null);
        _currencyRateProvider.GetRateAsync("USD", "INR", effectiveDate, Arg.Any<CancellationToken>()).Returns(83m);

        var handler = CreateHandler();
        var command = new AddExpenseLineCommand(claim.Id.Value, "Travel", 100m, Currency.Usd, effectiveDate, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var line = claim.Lines.Single();
        line.ConvertedAmount.Should().Be(Money.Of(8300m, Currency.Inr));
        line.ExchangeRate.Should().Be(83m);
        await _writeCurrencyRates.Received(1).AddAsync(
            Arg.Is<CurrencyRate>(r => r.FromCurrency == Currency.Usd && r.ToCurrency == Currency.Inr && r.Rate == 83m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Claim_Is_Not_Found()
    {
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((ExpenseClaim?)null);
        var handler = CreateHandler();
        var command = new AddExpenseLineCommand(Guid.NewGuid(), "Travel", 100m, Currency.Inr, new DateOnly(2026, 1, 10), null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
