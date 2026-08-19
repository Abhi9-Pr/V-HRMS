using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password, string DeviceId, string? TotpCode) : IRequest<Result<LoginResult>>;

public sealed record LoginResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
