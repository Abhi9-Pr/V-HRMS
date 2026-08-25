using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Eis;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Eis;

public class GetOffboardingChecklistByEmployeeIdQueryHandlerTests
{
    private readonly IReadRepository<OffboardingChecklist> _checklists = Substitute.For<IReadRepository<OffboardingChecklist>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetOffboardingChecklistByEmployeeIdQueryHandlerTests() => _tenantContext.TenantId.Returns(_tenantId);

    private GetOffboardingChecklistByEmployeeIdQueryHandler CreateHandler() => new(_checklists, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_The_Checklist_When_Found()
    {
        var employeeId = EmployeeId.New();
        var checklist = OffboardingChecklist.Initiate(_tenantId, employeeId, new DateOnly(2026, 6, 1), DateTimeOffset.UtcNow, "hr@vespera.test").Value;
        _checklists.FirstOrDefaultAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var result = await CreateHandler().Handle(new GetOffboardingChecklistByEmployeeIdQuery(employeeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmployeeId.Should().Be(employeeId.Value);
        result.Value.AccessRevokedStatus.Should().Be("Pending");
        result.Value.AssetsRecoveredStatus.Should().Be("Pending");
        result.Value.FinalSettlementStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_No_Checklist_Exists()
    {
        _checklists.FirstOrDefaultAsync(Arg.Any<ISpecification<OffboardingChecklist>>(), Arg.Any<CancellationToken>())
            .Returns((OffboardingChecklist?)null);

        var result = await CreateHandler().Handle(new GetOffboardingChecklistByEmployeeIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("offboarding_checklist.not_found");
    }
}
