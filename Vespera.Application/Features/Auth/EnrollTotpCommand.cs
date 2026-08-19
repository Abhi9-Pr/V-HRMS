using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Auth;

public sealed record EnrollTotpCommand : IRequest<Result<EnrollTotpResult>>;

public sealed record EnrollTotpResult(string Secret, string QrCodeUri);
