using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record DeleteEmployeeDocumentCommand(Guid EmployeeId, Guid DocumentId) : IRequest<Result>;
