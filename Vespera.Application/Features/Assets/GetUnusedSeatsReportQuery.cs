using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record GetUnusedSeatsReportQuery : IRequest<Result<IReadOnlyList<UnusedSeatsReportRowDto>>>;

public sealed record UnusedSeatsReportRowDto(Guid LicenseId, string ProductName, int SeatCount, int SeatsUsed, int UnusedSeats, DateOnly? ExpiresAt);
