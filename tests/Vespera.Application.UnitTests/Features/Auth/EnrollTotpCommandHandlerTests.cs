using FluentAssertions;
using NSubstitute;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Auth;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Auth;

public class EnrollTotpCommandHandlerTests
{
    private readonly IUserCredentialStore _credentialStore = Substitute.For<IUserCredentialStore>();
    private readonly IOtpService _otpService = Substitute.For<IOtpService>();
    private readonly IReadRepository<User> _users = Substitute.For<IReadRepository<User>>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private EnrollTotpCommandHandler CreateHandler() => new(_credentialStore, _otpService, _users, _currentUser);

    [Fact]
    public async Task Handle_Should_Return_Secret_And_QrCodeUri()
    {
        var userId = Guid.NewGuid();
        _currentUser.UserId.Returns(userId);
        var email = EmailAddress.Create("finance.admin@vespera.test").Value;
        var user = User.Create(TenantId.New(), email, employeeId: null, DateTimeOffset.UtcNow, "system");
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns(user);
        _credentialStore.BeginTwoFactorEnrollmentAsync(new UserId(userId), Arg.Any<CancellationToken>()).Returns("SECRET123");
        _otpService.GenerateQrCodeUri("SECRET123", email.Value).Returns("otpauth://totp/uri");

        var result = await CreateHandler().Handle(new EnrollTotpCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Secret.Should().Be("SECRET123");
        result.Value.QrCodeUri.Should().Be("otpauth://totp/uri");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_Not_Authenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await CreateHandler().Handle(new EnrollTotpCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.not_authenticated");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_User_Not_Found()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _users.FirstOrDefaultAsync(Arg.Any<UserByIdSpecification>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new EnrollTotpCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.user_not_found");
    }
}
