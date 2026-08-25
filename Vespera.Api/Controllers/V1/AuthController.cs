using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Vespera.Api.Authorization;
using Vespera.Api.Extensions;
using Vespera.Api.Http;
using Vespera.Application.Authorization;
using Vespera.Application.Features.Auth;

namespace Vespera.Api.Controllers.V1;

/// <summary>Login/refresh/logout, password reset, and TOTP enrolment. Login, refresh, and
/// password-reset endpoints resolve the tenant from the pre-auth <c>X-Tenant-Id</c> header — see
/// HttpTenantContext.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting(ObservabilityServiceCollectionExtensions.AuthRateLimitPolicyName)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Admin-only — creates a new user in the current tenant.</summary>
    /// <response code="200">The new user's id.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">The caller lacks Users.Manage.</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost("register")]
    [HasPermission(Permissions.Users.Manage)]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this, id => Ok(new RegisterUserResponse(id)));

    /// <response code="200">Access + refresh tokens.</response>
    /// <response code="401">Invalid credentials, or a required authenticator code is missing/invalid.</response>
    /// <response code="403">Account not active, or two-factor enrolment is required before this account can sign in.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <summary>Rotates a refresh token. Presenting a token that was already rotated away revokes
    /// every token in its family (reuse detection).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAllDevices(CancellationToken cancellationToken) =>
        (await _sender.Send(new LogoutAllDevicesCommand(), cancellationToken)).ToActionResult(this);

    /// <summary>Always returns 204 whether or not the email matches an account — see
    /// ForgotPasswordCommandHandler for why.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);

    /// <summary>Begins TOTP enrolment: generates a new (unconfirmed) secret and returns it plus
    /// an otpauth:// URI to render as a QR code. Two-factor stays disabled until confirmed.</summary>
    [HttpPost("totp/enroll")]
    [Authorize]
    [ProducesResponseType(typeof(EnrollTotpResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnrollTotp(CancellationToken cancellationToken) =>
        (await _sender.Send(new EnrollTotpCommand(), cancellationToken)).ToActionResult(this);

    [HttpPost("totp/confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmTotpEnrollment(ConfirmTotpEnrollmentCommand command, CancellationToken cancellationToken) =>
        (await _sender.Send(command, cancellationToken)).ToActionResult(this);
}

/// <summary>Response shape for <see cref="AuthController.Register"/> — kept out of Application
/// since it's an HTTP-layer response, not a command/query DTO.</summary>
public sealed record RegisterUserResponse(Guid Id);
