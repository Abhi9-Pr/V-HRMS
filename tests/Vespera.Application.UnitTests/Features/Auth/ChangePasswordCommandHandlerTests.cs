using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ChangePasswordCommandHandler CreateHandler() => new(_credentialStore, _currentUser);

    [Fact]
    public async Task Handle_Should_Delegate_To_CredentialStore_When_Authenticated()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _credentialStore.ChangePasswordAsync(new UserId(userId), "old-password", "new-password-123", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(new ChangePasswordCommand("old-password", "new-password-123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _credentialStore.Received(1).ChangePasswordAsync(new UserId(userId), "old-password", "new-password-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new ChangePasswordCommand("old-password", "new-password-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.not_authenticated");
        await _credentialStore.DidNotReceive().ChangePasswordAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
