using MediatR;
using Vespera.Application.Common;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Assets;

public sealed record GetSoftwareLicensesQuery(PagedRequest Paging) : IRequest<Result<PagedResult<SoftwareLicenseDto>>>;

public sealed record SoftwareLicenseDto(Guid Id, string ProductName, int SeatCount, int SeatsUsed, DateOnly? ExpiresAt);
