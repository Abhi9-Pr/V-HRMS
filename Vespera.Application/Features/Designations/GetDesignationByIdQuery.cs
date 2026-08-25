using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

public sealed record GetDesignationByIdQuery(Guid Id) : IRequest<Result<DesignationDto>>;
