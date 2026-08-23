using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

public sealed record DeleteDesignationCommand(Guid Id) : IRequest<Result>;
