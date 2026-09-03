using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Common;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class GetMyExpenseClaimsQueryHandlerTests
{
    private readonly IReadRepository<ExpenseClaim> _expenseClaims = Substitute.For<IReadRepository<ExpenseClaim>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    public GetMyExpenseClaimsQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetMyExpenseClaimsQueryHandler CreateHandler(bool hasLinkedEmployee = true)
    {
        var resolver = hasLinkedEmployee
            ? CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId)
            : new CurrentEmployeeResolver(Substitute.For<IReadRepository<User>>(), Substitute.For<ICurrentUser>());

        return new GetMyExpenseClaimsQueryHandler(_expenseClaims, _tenantContext, resolver);
    }

    [Fact]
    public async Task Handle_Should_Return_A_Page_Of_The_Current_Employees_Claims()
    {
        var claim = ExpenseClaim.Open(_tenantId, _employeeId, Currency.Inr);
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), null);

        _expenseClaims.ListAsync(Arg.Any<ExpenseClaimsByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns([claim]);
        _expenseClaims.CountAsync(Arg.Any<ExpenseClaimsByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetMyExpenseClaimsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(dto => dto.Id == claim.Id.Value);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_Account_Has_No_Linked_Employee()
    {
        var handler = CreateHandler(hasLinkedEmployee: false);
        var result = await handler.Handle(new GetMyExpenseClaimsQuery(new PagedRequest(1, 20, null, false)), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.no_employee");
    }
}
