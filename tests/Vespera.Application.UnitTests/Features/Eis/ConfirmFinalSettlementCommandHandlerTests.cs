using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class ConfirmFinalSettlementCommandHandlerTests
{
    private readonly IReadRepository<OffboardingChecklist> _checklists = Substitute.For<IReadRepository<OffboardingChecklist>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public ConfirmFinalSettlementCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private ConfirmFinalSettlementCommandHandler CreateHandler() => new(_checklists, _tenantContext, _currentUser, _dateTimeProvider);

    private OffboardingChecklist CreateChecklist() =>
        OffboardingChecklist.Initiate(_tenantId, EmployeeId.New(), new DateOnly(2026, 6, 1), DateTimeOffset.UtcNow, "hr@vespera.test").Value;

    [Fact]
    public async Task Handle_Should_Mark_Final_Settlement_Processed_When_Checklist_Exists()
    {
        var checklist = CreateChecklist();
        _checklists.FirstOrDefaultAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var result = await CreateHandler().Handle(new ConfirmFinalSettlementCommand(checklist.EmployeeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        checklist.FinalSettlementStatus.Should().Be(ChecklistItemStatus.Done);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_No_Checklist_Exists()
    {
        _checklists.FirstOrDefaultAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>())
            .Returns((OffboardingChecklist?)null);

        var result = await CreateHandler().Handle(new ConfirmFinalSettlementCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("offboarding_checklist.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Conflict_When_Already_Confirmed()
    {
        var checklist = CreateChecklist();
        checklist.MarkFinalSettlementProcessed(DateTimeOffset.UtcNow, "hr@vespera.test");
        _checklists.FirstOrDefaultAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var result = await CreateHandler().Handle(new ConfirmFinalSettlementCommand(checklist.EmployeeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("offboarding_checklist.final_settlement_already_processed");
    }
}
