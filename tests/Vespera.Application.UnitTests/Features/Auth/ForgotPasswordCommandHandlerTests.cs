using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly INotificationDispatcher _notificationDispatcher = Substitute.For<INotificationDispatcher>();

    private ForgotPasswordCommandHandler CreateHandler() => new(_users, _credentialStore, _notificationDispatcher);

    [Fact]
    public async Task Handle_Should_Dispatch_Reset_Notification_When_User_Exists()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = User.Create(TenantId.New(), email, employeeId: null, DateTimeOffset.UtcNow, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialStore.GeneratePasswordResetTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns("reset-token");

        var result = await CreateHandler().Handle(new ForgotPasswordCommand("user@vespera.test"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notificationDispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationMessage>(m => m.Body.Contains("reset-token")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Succeed_Without_Dispatching_When_User_Not_Found()
    {
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new ForgotPasswordCommand("unknown@vespera.test"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notificationDispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Succeed_Without_Dispatching_When_Email_Is_Malformed()
    {
        var result = await CreateHandler().Handle(new ForgotPasswordCommand("not-an-email"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notificationDispatcher.DidNotReceive().DispatchAsync(Arg.Any<NotificationMessage>(), Arg.Any<CancellationToken>());
    }
}
