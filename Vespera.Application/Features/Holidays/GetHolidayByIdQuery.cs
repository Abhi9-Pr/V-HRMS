using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed record GetHolidayByIdQuery(Guid Id) : IRequest<Result<HolidayDto>>;
