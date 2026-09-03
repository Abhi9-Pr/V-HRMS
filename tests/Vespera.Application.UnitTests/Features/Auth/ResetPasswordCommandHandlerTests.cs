using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();

    private ResetPasswordCommandHandler CreateHandler() => new(_users, _credentialStore);

    [Fact]
    public async Task Handle_Should_Reset_The_Password_When_User_Exists()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = User.Create(TenantId.New(), email, employeeId: null, DateTimeOffset.UtcNow, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialStore.ResetPasswordAsync(user.Id, "reset-token", "new-password-123", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await CreateHandler().Handle(new ResetPasswordCommand("user@vespera.test", "reset-token", "new-password-123"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _credentialStore.Received(1).ResetPasswordAsync(user.Id, "reset-token", "new-password-123", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Email_Is_Malformed()
    {
        var result = await CreateHandler().Handle(new ResetPasswordCommand("not-an-email", "reset-token", "new-password-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_reset_token");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Does_Not_Exist()
    {
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new ResetPasswordCommand("unknown@vespera.test", "reset-token", "new-password-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_reset_token");
    }

    [Fact]
    public async Task Handle_Should_Propagate_Failure_From_CredentialStore()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        var user = User.Create(TenantId.New(), email, employeeId: null, DateTimeOffset.UtcNow, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByEmailSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialStore.ResetPasswordAsync(user.Id, "bad-token", "new-password-123", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Unauthorized("auth.invalid_reset_token", "This reset token is not valid.")));

        var result = await CreateHandler().Handle(new ResetPasswordCommand("user@vespera.test", "bad-token", "new-password-123"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_reset_token");
    }
}
