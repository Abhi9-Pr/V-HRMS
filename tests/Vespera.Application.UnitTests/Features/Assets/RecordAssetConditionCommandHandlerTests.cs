using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class RecordAssetConditionCommandHandlerTests
{
    private readonly IReadRepository<AssetAssignment> _assignments = Substitute.For<IReadRepository<AssetAssignment>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public RecordAssetConditionCommandHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private RecordAssetConditionCommandHandler CreateHandler() => new(_assignments, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Append_Condition_Report()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);
        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(assignment);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RecordAssetConditionCommand(assignment.Id.Value, AssetConditionRating.Good, "Minor wear", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        assignment.ConditionReports.Should().ContainSingle(r => r.Rating == AssetConditionRating.Good);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Assignment_Not_Found()
    {
        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((AssetAssignment?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(
            new RecordAssetConditionCommand(Guid.NewGuid(), AssetConditionRating.Good, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
