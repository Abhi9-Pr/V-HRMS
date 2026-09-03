using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class GetExpenseClaimByIdQueryHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    private GetExpenseClaimByIdQueryHandler CreateHandler() =>
        new(_expenseClaims, CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));

    [Fact]
    public async Task Handle_Should_Return_The_Claim_When_Owned_By_The_Current_Employee()
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpenseClaimByIdQuery(claim.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(claim.Id.Value);
        result.Value.Lines.Should().ContainSingle(line => line.Category == "Travel" && line.Amount == 1500m);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Claim_Not_Found()
    {
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((ExpenseClaim?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpenseClaimByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Claim_Belongs_To_A_Different_Employee()
    {
        var claim = ExpenseClaim.Open(_tenantId, EmployeeId.New(), Currency.Inr);
        _expenseClaims.FirstOrDefaultAsync(Arg.Any<ExpenseClaimByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(claim);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetExpenseClaimByIdQuery(claim.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_found");
    }
}
