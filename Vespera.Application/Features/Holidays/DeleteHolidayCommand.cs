using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed record DeleteHolidayCommand(Guid Id) : IRequest<Result>;
