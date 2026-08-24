using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Expenses;

public class OpenExpenseClaimCommandHandlerTests
{
    private readonly IWriteRepository<ExpenseClaim> _expenseClaims = Substitute.For<IWriteRepository<ExpenseClaim>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly EmployeeId _employeeId = EmployeeId.New();

    public OpenExpenseClaimCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    [Fact]
    public async Task Handle_Should_Open_A_Claim_And_Return_Its_Id()
    {
        var handler = new OpenExpenseClaimCommandHandler(
            _expenseClaims, _tenantContext, CurrentEmployeeTestSupport.CreateResolver(_tenantId, _employeeId));
        var command = new OpenExpenseClaimCommand(Currency.Inr, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _expenseClaims.Received(1).AddAsync(
            Arg.Is<ExpenseClaim>(c => c.EmployeeId == _employeeId && c.SettlementCurrency == Currency.Inr), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Signed_In_Account_Has_No_Linked_Employee()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var users = Substitute.For<IReadRepository<User>>();
        var resolver = new CurrentEmployeeResolver(users, currentUser);

        var handler = new OpenExpenseClaimCommandHandler(_expenseClaims, _tenantContext, resolver);
        var command = new OpenExpenseClaimCommand(Currency.Inr, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _expenseClaims.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
