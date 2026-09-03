using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Expenses.Policy;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses.Policy;

public class GetExpensePoliciesQueryHandlerTests
{
    private readonly IReadRepository<ExpensePolicy> _expensePolicies = Substitute.For<IReadRepository<ExpensePolicy>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetExpensePoliciesQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetExpensePoliciesQueryHandler CreateHandler() => new(_expensePolicies, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_Expense_Policies()
    {
        var policy = ExpensePolicy.Create(
            _tenantId, "Travel", Money.Of(5000m, Currency.Inr), Money.Of(500m, Currency.Inr), DateTimeOffset.UtcNow, "system").Value;

        _expensePolicies.ListAsync(Arg.Any<ExpensePoliciesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns([policy]);
        _expensePolicies.CountAsync(Arg.Any<ExpensePoliciesPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpensePoliciesQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == policy.Id.Value && dto.Category == "Travel" && dto.MaxAmountPerClaim == 5000m);
        result.Value.TotalCount.Should().Be(1);
    }
}
