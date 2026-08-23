using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record RejectEmployeeDocumentCommand(Guid EmployeeId, Guid DocumentId, string Reason) : IRequest<Result>;
