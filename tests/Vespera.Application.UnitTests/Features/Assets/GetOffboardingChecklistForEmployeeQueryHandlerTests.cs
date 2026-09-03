using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using OffboardingChecklist = Vespera.Domain.Assets.OffboardingChecklist;

namespace Vespera.Application.UnitTests.Features.Assets;

public class GetOffboardingChecklistForEmployeeQueryHandlerTests
{
    private readonly IReadRepository<OffboardingChecklist> _checklists = Substitute.For<IReadRepository<OffboardingChecklist>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantId _tenantId = TenantId.New();

    public GetOffboardingChecklistForEmployeeQueryHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
    }

    private GetOffboardingChecklistForEmployeeQueryHandler CreateHandler() => new(_checklists, _tenantContext);

    [Fact]
    public async Task Handle_Should_Return_The_Checklist_With_Its_Items()
    {
        var employeeId = EmployeeId.New();
        var checklist = OffboardingChecklist.Create(_tenantId, employeeId, ["Return laptop"]);

        _checklists.FirstOrDefaultAsync(Arg.Any<OffboardingChecklistByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetOffboardingChecklistForEmployeeQuery(employeeId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmployeeId.Should().Be(employeeId.Value);
        result.Value.Items.Should().ContainSingle(item => item.Description == "Return laptop" && !item.IsComplete);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_No_Checklist_Exists_For_The_Employee()
    {
        _checklists.FirstOrDefaultAsync(Arg.Any<OffboardingChecklistByEmployeeSpecification>(), Arg.Any<CancellationToken>()).Returns((OffboardingChecklist?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new GetOffboardingChecklistForEmployeeQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("offboarding_checklist.not_found");
    }
}
