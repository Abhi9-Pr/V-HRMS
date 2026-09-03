using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Application.Features.Payroll;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class GetInvestmentDeclarationReviewQueueQueryHandlerTests
{
    private readonly IReadRepository<InvestmentDeclaration> _declarations = Substitute.For<IReadRepository<InvestmentDeclaration>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IPiiAccessAuditor _piiAccessAuditor = Substitute.For<IPiiAccessAuditor>();
    private readonly TenantId _tenantId = TenantId.New();

    private GetInvestmentDeclarationReviewQueueQueryHandler CreateHandler() =>
        new(_declarations, _tenantContext, _currentUser, _dateTimeProvider, _piiAccessAuditor);

    [Fact]
    public async Task Handle_Should_Return_A_Paged_Queue_With_Pending_Line_Counts_And_Record_Pii_Access()
    {
        var now = DateTimeOffset.UtcNow;
        var window = InvestmentDeclarationWindow.Create(_tenantId, "2026-27", new DateOnly(2026, 4, 1), new DateOnly(2027, 3, 31)).Value;
        var declaration = InvestmentDeclaration.Create(
            _tenantId, EmployeeId.New(), TaxRegimeVersionId.New(), "2026-27", new DateOnly(2026, 4, 1), window).Value;
        declaration.AddLine("80C", Money.Of(50000m, Currency.Inr), null, new DateOnly(2026, 4, 1), window);
        declaration.AddLine("80D", Money.Of(25000m, Currency.Inr), null, new DateOnly(2026, 4, 1), window);
        declaration.Submit(new DateOnly(2026, 4, 1), window);
        declaration.ReviewLine(0, true, null, "reviewer", now);

        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)null);
        _dateTimeProvider.UtcNow.Returns(now);
        _declarations.ListAsync(Arg.Any<SubmittedInvestmentDeclarationsPagedSpecification>(), Arg.Any<CancellationToken>())
            .Returns([declaration]);
        _declarations.CountAsync(Arg.Any<SubmittedInvestmentDeclarationsPagedSpecification>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await CreateHandler().Handle(new GetInvestmentDeclarationReviewQueueQuery(new PagedRequest(1, 20)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(item => item.Id == declaration.Id.Value && item.LineCount == 2 && item.PendingLineCount == 1);
        await _piiAccessAuditor.Received(1).RecordAccessAsync(
            _tenantId, "InvestmentDeclaration", Guid.Empty, "ReviewQueue", "system", now, Arg.Any<CancellationToken>());
    }
}
