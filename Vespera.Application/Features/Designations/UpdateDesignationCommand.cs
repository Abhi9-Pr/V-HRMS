using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Designations;

public sealed record UpdateDesignationCommand(Guid Id, string Title, int Grade) : IRequest<Result>;
