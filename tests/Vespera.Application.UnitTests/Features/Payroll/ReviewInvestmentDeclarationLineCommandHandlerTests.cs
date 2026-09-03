using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class ReviewInvestmentDeclarationLineCommandHandlerTests
{
    private readonly IReadRepository<InvestmentDeclaration> _declarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    private ReviewInvestmentDeclarationLineCommandHandler CreateHandler() => new(_declarations, _currentUser, _dateTimeProvider);

    private InvestmentDeclaration SubmittedDeclarationWithOneLine()
    {
        var window = InvestmentDeclarationWindow.Create(_tenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)).Value;
        var declaration = InvestmentDeclaration.Create(
            _tenantId, EmployeeId.New(), TaxRegimeVersionId.New(), "2026-27", new DateOnly(2026, 4, 1), window).Value;
        declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), null, new DateOnly(2026, 4, 1), window);
        declaration.Submit(new DateOnly(2026, 4, 1), window);
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(declaration);
        return declaration;
    }

    [Fact]
    public async Task Handle_Should_Approve_The_Line_And_Auto_Verify_When_It_Was_The_Last_Pending_Line()
    {
        var now = DateTimeOffset.UtcNow;
        var declaration = SubmittedDeclarationWithOneLine();
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await CreateHandler().Handle(
            new ReviewInvestmentDeclarationLineCommand(declaration.Id.Value, 0, true, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        declaration.Status.Should().Be(InvestmentDeclarationStatus.Verified);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Rejecting_Without_A_Comment()
    {
        var now = DateTimeOffset.UtcNow;
        var declaration = SubmittedDeclarationWithOneLine();
        _dateTimeProvider.UtcNow.Returns(now);

        var result = await CreateHandler().Handle(
            new ReviewInvestmentDeclarationLineCommand(declaration.Id.Value, 0, false, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.rejection_comment_required");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_The_Declaration_Does_Not_Exist()
    {
        _declarations.FirstOrDefaultAsync(Arg.Any<InvestmentDeclarationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((InvestmentDeclaration?)null);

        var result = await CreateHandler().Handle(
            new ReviewInvestmentDeclarationLineCommand(Guid.NewGuid(), 0, true, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("investment_declaration.not_found");
    }
}
