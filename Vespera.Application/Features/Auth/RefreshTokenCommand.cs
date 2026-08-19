using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken, string DeviceId) : IRequest<Result<LoginResult>>;
