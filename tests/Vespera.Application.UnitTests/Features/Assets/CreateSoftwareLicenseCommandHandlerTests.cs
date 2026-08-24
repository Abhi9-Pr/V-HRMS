using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Assets;
using Vespera.Domain.Assets;
using Vespera.Domain.Common;

namespace Vespera.Application.UnitTests.Features.Assets;

public class CreateSoftwareLicenseCommandHandlerTests
{
    private readonly IWriteRepository<SoftwareLicense> _licenses = Substitute.For<IWriteRepository<SoftwareLicense>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public CreateSoftwareLicenseCommandHandlerTests()
    {
        _tenantContext.TenantId.Returns(TenantId.New());
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    private CreateSoftwareLicenseCommandHandler CreateHandler() => new(_licenses, _tenantContext, _currentUser, _dateTimeProvider);

    [Fact]
    public async Task Handle_Should_Add_License_And_Return_Its_Id_On_Success()
    {
        var handler = CreateHandler();
        var command = new CreateSoftwareLicenseCommand("Figma", 10, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _licenses.Received(1).AddAsync(Arg.Is<SoftwareLicense>(l => l.ProductName == "Figma" && l.SeatCount == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_SeatCount_Is_Not_Positive()
    {
        var handler = CreateHandler();
        var command = new CreateSoftwareLicenseCommand("Figma", 0, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}
