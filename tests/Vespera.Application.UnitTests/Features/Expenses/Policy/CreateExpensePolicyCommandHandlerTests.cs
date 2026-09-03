using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class CreateExpensePolicyCommandHandlerTests
{
    private readonly IWriteRepository<ExpensePolicy> _expensePolicies = Substitute.For<IWriteRepository<ExpensePolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public CreateExpensePolicyCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
    }

    private CreateExpensePolicyCommandHandler CreateHandler() => new(_expensePolicies, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Create_The_Policy_And_Add_It()
    {
        var handler = CreateHandler();
        var command = new CreateExpensePolicyCommand(
            "Travel", 5000m, 500m, Currency.Inr, null, ExpensePolicySeverity.Block, ExpensePolicySeverity.Warn, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _expensePolicies.Received(1).AddAsync(
            Arg.Is<ExpensePolicy>(p => p.Category == "Travel" && p.Id.Value == result.Value), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Category_Is_Blank()
    {
        var handler = CreateHandler();
        var command = new CreateExpensePolicyCommand(
            "   ", 5000m, 500m, Currency.Inr, null, ExpensePolicySeverity.Block, ExpensePolicySeverity.Warn, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _expensePolicies.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
