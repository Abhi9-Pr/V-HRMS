using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
