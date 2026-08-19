using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record ResetPasswordCommand(string Email, string ResetToken, string NewPassword) : IRequest<Result>;
