using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CompleteOffboardingChecklistItemCommandHandlerTests
{
    private readonly IReadRepository<OffboardingChecklist> _checklists = Substitute.For<IReadRepository<OffboardingChecklist>>();

    private CompleteOffboardingChecklistItemCommandHandler CreateHandler() => new(_checklists);

    [Fact]
    public async Task Handle_Should_Complete_The_Item()
    {
        var checklist = OffboardingChecklist.Create(TenantId.New(), EmployeeId.New(), ["Recover asset X"]);
        _checklists.FirstOrDefaultAsync(Arg.Any<OffboardingChecklistByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompleteOffboardingChecklistItemCommand(checklist.Id.Value, 0, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        checklist.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Index_Out_Of_Range()
    {
        var checklist = OffboardingChecklist.Create(TenantId.New(), EmployeeId.New(), ["Recover asset X"]);
        _checklists.FirstOrDefaultAsync(Arg.Any<OffboardingChecklistByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(checklist);

        var handler = CreateHandler();
        var result = await handler.Handle(new CompleteOffboardingChecklistItemCommand(checklist.Id.Value, 5, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
