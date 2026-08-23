using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Holidays;

public sealed record UpdateHolidayCommand(Guid Id, string Name, DateOnly Date) : IRequest<Result>;
