using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ConfirmTotpEnrollmentCommandHandlerTests
{
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ConfirmTotpEnrollmentCommandHandler CreateHandler() => new(_credentialStore, _currentUser);

    [Fact]
    public async Task Handle_Should_Delegate_To_CredentialStore_When_Authenticated()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        _credentialStore.ConfirmTwoFactorEnrollmentAsync(new UserId(userId), "123456", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(new ConfirmTotpEnrollmentCommand("123456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _credentialStore.Received(1).ConfirmTwoFactorEnrollmentAsync(new UserId(userId), "123456", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new ConfirmTotpEnrollmentCommand("123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.not_authenticated");
    }
}
