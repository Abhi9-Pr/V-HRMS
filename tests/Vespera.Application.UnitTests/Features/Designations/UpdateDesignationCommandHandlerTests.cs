using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Designations;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Application.UnitTests.Features.Designations;

public class UpdateDesignationCommandHandlerTests
{
    private readonly IReadRepository<Designation> _designations = Substitute.For<IReadRepository<Designation>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly TenantId _tenantId = TenantId.New();

    public UpdateDesignationCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private UpdateDesignationCommandHandler CreateHandler() => new(_designations, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Rename_And_Change_Grade_When_Designation_Exists()
    {
        var designation = Designation.Create(_tenantId, "Software Engineer", 3, DateTimeOffset.UtcNow, "seed").Value;
        _designations.FirstOrDefaultAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>()).Returns(designation);

        var handler = CreateHandler();
        var command = new UpdateDesignationCommand(designation.Id.Value, "Senior Software Engineer", 4);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        designation.Title.Should().Be("Senior Software Engineer");
        designation.Grade.Should().Be(4);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Designation_Missing()
    {
        _designations.FirstOrDefaultAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>()).Returns((Designation?)null);

        var handler = CreateHandler();
        var result = await handler.Handle(new UpdateDesignationCommand(Guid.NewGuid(), "Software Engineer", 3), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("designation.not_found");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Grade_Is_Not_Positive()
    {
        var designation = Designation.Create(_tenantId, "Software Engineer", 3, DateTimeOffset.UtcNow, "seed").Value;
        _designations.FirstOrDefaultAsync(Arg.Any<ISpecification<Designation>>(), Arg.Any<CancellationToken>()).Returns(designation);

        var handler = CreateHandler();
        var command = new UpdateDesignationCommand(designation.Id.Value, "Software Engineer", 0);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("designation.invalid_grade");
    }
}
