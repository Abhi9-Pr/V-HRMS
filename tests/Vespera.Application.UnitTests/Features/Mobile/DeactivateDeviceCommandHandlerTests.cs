using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Mobile;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Mobile;

public class DeactivateDeviceCommandHandlerTests
{
    private readonly IReadRepository<DeviceRegistration> _readRepository = Substitute.For<IReadRepository<DeviceRegistration>>();
    private readonly IWriteRepository<DeviceRegistration> _writeRepository = Substitute.For<IWriteRepository<DeviceRegistration>>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TenantId _tenantId = TenantId.New();

    private DeactivateDeviceCommandHandler CreateHandler() => new(_readRepository, _writeRepository, _tenantContext, _currentUser);

    [Fact]
    public async Task Handle_Should_Fail_When_Registration_Not_Found()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns((DeviceRegistration?)null);

        var result = await CreateHandler().Handle(new DeactivateDeviceCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.not_found");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Caller_Does_Not_Own_The_Registration()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var registration = DeviceRegistration.Register(_tenantId, UserId.New(), "device-1", DevicePlatform.Ios, "token").Value;
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(registration);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new DeactivateDeviceCommand(registration.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.not_owner");
    }

    [Fact]
    public async Task Handle_Should_Deactivate_The_Callers_Own_Device()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var userId = UserId.New();
        var registration = DeviceRegistration.Register(_tenantId, userId, "device-1", DevicePlatform.Ios, "token").Value;
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(registration);
        _currentUser.UserId.Returns(userId.Value);

        var result = await CreateHandler().Handle(new DeactivateDeviceCommand(registration.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        registration.IsActive.Should().BeFalse();
        _writeRepository.Received(1).Update(registration);
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Already_Inactive()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        var userId = UserId.New();
        var registration = DeviceRegistration.Register(_tenantId, userId, "device-1", DevicePlatform.Ios, "token").Value;
        registration.Deactivate();
        _readRepository.FirstOrDefaultAsync(Arg.Any<DeviceRegistrationByIdSpecification>(), Arg.Any<CancellationToken>())
            .Returns(registration);
        _currentUser.UserId.Returns(userId.Value);

        var result = await CreateHandler().Handle(new DeactivateDeviceCommand(registration.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("device_registration.already_inactive");
    }
}
