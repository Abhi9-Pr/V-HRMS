using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Assets;

public class UploadHandoverSignatureCommandHandlerTests
{
    private readonly IReadRepository<AssetAssignment> _assignments = Substitute.For<IReadRepository<AssetAssignment>>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();

    private UploadHandoverSignatureCommandHandler CreateHandler() => new(_assignments, _fileStorage);

    [Fact]
    public async Task Handle_Should_Upload_And_Capture_Signature_Reference()
    {
        var assignment = AssetAssignment.Assign(TenantId.New(), AssetId.New(), EmployeeId.New(), DateTimeOffset.UtcNow);
        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(assignment);
        _fileStorage.UploadAsync("sig.png", Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns("signatures/sig-key.png");

        var handler = CreateHandler();
        var result = await handler.Handle(
            new UploadHandoverSignatureCommand(assignment.Id.Value, [1, 2, 3], "sig.png", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("signatures/sig-key.png");
        assignment.HandoverSignatureReference.Should().Be("signatures/sig-key.png");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Assignment_Not_Found()
    {
        _assignments.FirstOrDefaultAsync(Arg.Any<AssetAssignmentByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((AssetAssignment?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UploadHandoverSignatureCommand(Guid.NewGuid(), [1], "sig.png", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
