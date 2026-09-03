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

public class AddInvestmentDeclarationLineCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IReadRepository<InvestmentDeclaration> _declarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly IWriteRepository<InvestmentDeclaration> _declarationWriter = Substitute.For<IWriteRepository<InvestmentDeclaration>>();
    private readonly IReadRepository<InvestmentDeclarationWindow> _windows = Substitute.For<IReadRepository<InvestmentDeclarationWindow>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private AddInvestmentDeclarationLineCommandHandler CreateHandler() =>
        new(_users, _declarations, _declarationWriter, _windows, _tenantContext, _currentUser, _dateTimeProvider);

    private void SetUpAuthenticatedUserWithEmployee(EmployeeId employeeId)
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var user = User.Create(_tenantId, EmailAddress.Create("employee@vespera.test").Value, employeeId, DateTimeOffset.UtcNow, "seed");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
    }

    [Fact]
    public async Task Handle_Should_Create_A_New_Declaration_And_Add_The_First_Line_When_None_Exists_Yet()
    {
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var employeeId = EmployeeId.New();
        SetUpAuthenticatedUserWithEmployee(employeeId);
        _dateTimeProvider.UtcNow.Returns(now);

        var window = InvestmentDeclarationWindow.Create(_tenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)).Value;
        _windows.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationWindowByTenantAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns(window);
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByEmployeeAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns((InvestmentDeclaration?)null);

        var command = new AddInvestmentDeclarationLineCommand("2026-27", Guid.NewGuid(), "80C", 50000m, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _declarationWriter.Received(1).AddAsync(
            Arg.Is<InvestmentDeclaration>(d => d.Lines.Count == 1 && d.Lines[0].Section == "80C"), Arg.Any<CancellationToken>());
        _declarationWriter.DidNotReceive().Update(Arg.Any<InvestmentDeclaration>());
    }

    [Fact]
    public async Task Handle_Should_Add_A_Line_To_An_Existing_Draft_Declaration_And_Update_It()
    {
        var now = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var employeeId = EmployeeId.New();
        SetUpAuthenticatedUserWithEmployee(employeeId);
        _dateTimeProvider.UtcNow.Returns(now);

        var window = InvestmentDeclarationWindow.Create(_tenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)).Value;
        var existing = InvestmentDeclaration.Create(
            _tenantId, employeeId, TaxRegimeVersionId.New(), "2026-27", new DateOnly(2026, 4, 1), window).Value;

        _windows.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationWindowByTenantAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns(window);
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByEmployeeAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var command = new AddInvestmentDeclarationLineCommand("2026-27", Guid.NewGuid(), "80D", 25000m, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.Lines.Should().ContainSingle(line => line.Section == "80D");
        _declarationWriter.Received(1).Update(existing);
        await _declarationWriter.DidNotReceive().AddAsync(Arg.Any<InvestmentDeclaration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Window_Is_Configured()
    {
        SetUpAuthenticatedUserWithEmployee(EmployeeId.New());
        _windows.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationWindowByTenantAndFySpecification>(), Arg.Any<CancellationToken>())
            .Returns((InvestmentDeclarationWindow?)null);

        var command = new AddInvestmentDeclarationLineCommand("2026-27", Guid.NewGuid(), "80C", 50000m, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.no_window");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var command = new AddInvestmentDeclarationLineCommand("2026-27", Guid.NewGuid(), "80C", 50000m, null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_authenticated");
    }
}
