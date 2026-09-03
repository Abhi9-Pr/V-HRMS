using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Mobile;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Mobile;

public class RegisterDeviceCommandHandlerTests
{
    private readonly IReadRepository<DeviceRegistration> _readRepository = Substitute.For<IReadRepository<DeviceRegistration>>();
    private readonly IWriteRepository<DeviceRegistration> _writeRepository = Substitute.For<IWriteRepository<DeviceRegistration>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();
    private readonly Guid _userIdValue = Guid.NewGuid();

    private RegisterDeviceCommandHandler CreateHandler() => new(_readRepository, _writeRepository, _tenantContext, _currentUser);

    private void ArrangeAuthenticatedUser()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userIdValue);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new RegisterDeviceCommand("device-1", DevicePlatform.Ios, "token-1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Register_A_New_Device_When_None_Exists()
    {
        ArrangeAuthenticatedUser();
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByUserAndDeviceIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((DeviceRegistration?)null);

        var result = await CreateHandler().Handle(new RegisterDeviceCommand("device-1", DevicePlatform.Ios, "token-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _writeRepository.Received(1).AddAsync(Arg.Is<DeviceRegistration>(d => d.DeviceId == "device-1" && d.PushToken == "token-1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Refresh_The_Push_Token_When_The_Device_Already_Exists()
    {
        ArrangeAuthenticatedUser();
        var existing = DeviceRegistration.Register(_tenantId, new UserId(_userIdValue), "device-1", DevicePlatform.Ios, "old-token").Value;
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByUserAndDeviceIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateHandler().Handle(new RegisterDeviceCommand("device-1", DevicePlatform.Ios, "new-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id.Value);
        existing.PushToken.Should().Be("new-token");
        _writeRepository.Received(1).Update(existing);
        await _writeRepository.DidNotReceive().AddAsync(Arg.Any<DeviceRegistration>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Reactivate_An_Inactive_Existing_Device()
    {
        ArrangeAuthenticatedUser();
        var existing = DeviceRegistration.Register(_tenantId, new UserId(_userIdValue), "device-1", DevicePlatform.Ios, "old-token").Value;
        existing.Deactivate();
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByUserAndDeviceIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateHandler().Handle(new RegisterDeviceCommand("device-1", DevicePlatform.Ios, "new-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeTrue();
    }
}
