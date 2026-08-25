using MediatR;
using Vespera.Domain.Common;

namespace Vespera.Application.Features.Employees.Documents;

public sealed record VerifyEmployeeDocumentCommand(Guid EmployeeId, Guid DocumentId) : IRequest<Result>;
