using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
