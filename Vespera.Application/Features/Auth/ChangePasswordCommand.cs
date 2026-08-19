using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
