using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Shifts;

public sealed record GetShiftByIdQuery(Guid Id) : IRequest<Result<ShiftDto>>;
