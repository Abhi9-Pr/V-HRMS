using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetMyInvestmentDeclarationQueryHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<InvestmentDeclaration> _declarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetMyInvestmentDeclarationQueryHandler CreateHandler() =>
        new(_users, _declarations, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    private void SetUpAuthenticatedUserWithEmployee(EmployeeId employeeId)
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, DateTimeOffset.UtcNow, "seed");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    [Fact]
    public async Task Handle_Should_Return_The_Callers_Declaration_As_A_Dto_And_Record_Pii_Access()
    {
        var now = DateTimeOffset.UtcNow;
        var employeeId = EmployeeId.New();
        SetUpAuthenticatedUserWithEmployee(employeeId);
        _dateTimeProvider.UtcNow.Returns(now);

        var window = InvestmentDeclarationWindow.Create(_tenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)).Value;
        var declaration = InvestmentDeclaration.Create(
            _tenantId, employeeId, TaxRegimeVersionId.New(), "2026-27", new DateOnly(2026, 4, 1), window).Value;
        declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), null, new DateOnly(2026, 4, 1), window);
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByEmployeeAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns(declaration);

        var result = await CreateHandler().Handle(new GetMyInvestmentDeclarationQuery("2026-27"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Lines.Should().ContainSingle(line => line.Section == "80C" && line.Amount == 50000m);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "InvestmentDeclaration", declaration.Id.Value, "Lines", Arg.Any<string>(), now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Return_Null_When_The_Caller_Has_No_Declaration_For_The_Financial_Year()
    {
        SetUpAuthenticatedUserWithEmployee(EmployeeId.New());
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByEmployeeAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns((InvestmentDeclaration?)null);

        var result = await CreateHandler().Handle(new GetMyInvestmentDeclarationQuery("2026-27"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new GetMyInvestmentDeclarationQuery("2026-27"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Caller_Has_No_Linked_Employee_Profile()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("no.employee@vespera.test").Value, null, DateTimeOffset.UtcNow, "seed");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await CreateHandler().Handle(new GetMyInvestmentDeclarationQuery("2026-27"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.no_employee_profile");
    }
}
